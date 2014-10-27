<#
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under MIT License. See license file.
#>
param(
    [Parameter(Mandatory=$true)][string]$JsonFilesFolder,
    [Parameter(Mandatory=$false)][string]$SubscriptionName="Current",
    [Parameter(Mandatory=$false)][string]$ResourceGroupName="ADF",
    [Parameter(Mandatory=$true)][string]$DataFactoryName,
    [Parameter(Mandatory=$false)][string]$Location="WestUS",
    [Parameter(Mandatory=$false)][string]$StartTime,
    [Parameter(Mandatory=$false)][string]$EndTime
)

function Extract-Name {
	param(
	    [string]$FileContent,
	    [string]$Target
	)

	$index = $fileContent.ToLower().IndexOf($target.ToLower())
	$str = $fileContent.Substring($index, $target.Length)
    $str
} 

# Use the AzureResourceManager Mode
Switch-AzureMode AzureResourceManager
cls

$oldErrors = $Error.Count

[System.Reflection.Assembly]::LoadWithPartialName("System.Web.Extensions")
$ser = New-Object System.Web.Script.Serialization.JavaScriptSerializer -ErrorAction Stop

if($SubscriptionName.CompareTo("Current"))
{
    Select-AzureSubscription $SubscriptionName -ErrorAction Stop
} else {
    Get-AzureSubscription -Current
}

Write-Host "Reading files from $JsonFilesFolder..." -ForegroundColor Green

$files = Get-ChildItem $JsonFilesFolder"\*" -Include *.json -Recurse -ErrorAction Stop

if($files.Count -eq 0)
{
    Throw "No files are found in the location specified. Please double-check the folder."
}

Write-Host "Creating Data Factory (update if exists)..."  -ForegroundColor Green
New-AzureDataFactory -ResourceGroupName $ResourceGroupName -Name $DataFactoryName -Location $Location -Force -ErrorAction Stop

Write-Host "Creating ADF LinkedServices..." -ForegroundColor Green
$files2 = @()

# Get the Azure Data Factory
$df = Get-AzureDataFactory -ResourceGroupName $ResourceGroupName -Name $DataFactoryName

foreach($file in $files)
{
    $file.FullName
    $json = Get-Content $file.FullName -Raw -ErrorAction Stop
    $obj = ""
    $obj = $ser.DeserializeObject($json.ToLower()) 

    if(-not $obj)
    {
            Throw "Json file not valid, please double check using a validator on file: $file"
    }


    if(-not $obj.properties.Keys.Contains('Type'.ToLower()))
    {
        continue;
    }

    if($obj.properties.Keys.Contains('LinkedServiceName'.ToLower()))
    {
        $files2 += $file
        continue;
    }

    New-AzureDataFactoryLinkedService -DataFactory $df -File $file.FullName -ErrorAction Stop -Force
}

foreach($file in $files2)
{
    $json = Get-Content $file.FullName -Raw
    $obj = $ser.DeserializeObject($json.ToLower())
    New-AzureDataFactoryLinkedService -DataFactory $df -File $file.FullName -ErrorAction Stop -Force
}

Write-Host "Creating ADF Tables..."  -ForegroundColor Green
foreach($file in $files)
{
    $json = Get-Content $file.FullName -Raw
    $obj = $ser.DeserializeObject($json.ToLower())

    if(-not $obj.properties.Keys.Contains('Location'.ToLower()) -or $obj.properties.Keys.Contains('Type'.ToLower()))
    {
        continue;
    }

    New-AzureDataFactoryTable -DataFactory $df -File $file.FullName -ErrorAction Stop -Force
}

Write-Host "Creating ADF Pipelines..."  -ForegroundColor Green
foreach($file in $files)
{
    $json = Get-Content $file.FullName -Raw
    $obj = $ser.DeserializeObject($json.ToLower())

    if(-not $obj.properties.Keys.Contains('Activities'.ToLower()))
    {
        continue;
    }

    New-AzureDataFactoryPipeline -DataFactory $df -File $file.FullName -ErrorAction Stop -Force

    if($StartTime -and $EndTime)
    {
        $name = Extract-Name -FileContent $json -Target $obj.name
        Write-Host "Setting Pipeline Active Period from [$StartTime] to [$EndTime]..."  -ForegroundColor Green
        Set-AzureDataFactoryPipelineActivePeriod -DataFactory $df -Name $name -StartDateTime $StartTime -EndDateTime $EndTime -Force 
    }
}

Write-Verbose "Note: You are currently in the AzureResourceManager Azure Mode." 
Write-Verbose "If you need to use other Azure Services (e.g. storage, HDInsight), you will need to Switch-AzureMode AzureServiceManagement."
 
Write-Host "Data Factory ["$DataFactoryName" ] Deployment Summary"  -ForegroundColor Green
$numErrors =  $Error.Count - $oldErrors
if ( $numErrors > 0 )
{
  Write-Host "Status: Failed ($numErrors errors occured during deployment) " -ForegroundColor Red
}
else {
  Write-Host "Status: Success"  -ForegroundColor Green
}



