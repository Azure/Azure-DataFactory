param([String]$resourcegroupname="dffunctionssample", [String] $uniqueresourcenameprefix = "")

if ( [String]::IsNullOrWhiteSpace($uniqueresourcenameprefix))
{
    #Change this to a fixed value to override existing resources instaed of creating new ones on each deployment, for example:
    #$uniqueresourcenameprefix = "e1064086576241d39"
    $uniqueresourcenameprefix =  [System.Guid]::NewGuid().ToString("N").Substring(0,17)
}

if ($uniqueresourcenameprefix.Length -gt 17)
{
    "Truncating uniqueresourcenameprefix to 17 characters."
    $uniqueresourcenameprefix = $uniqueresourcenameprefix.Substring(0,17)
}

echo "Creating resource group with name " $resourcegroupname

New-AzResourceGroup -Name $resourcegroupname -Location "West US"

echo "Deploying app resources"

$FunctionApp_Name = $uniqueresourcenameprefix + "app"
$StorageAccount_Name = $uniqueresourcenameprefix + "storage"
$DataFactory_Name = $uniqueresourcenameprefix + "factory"

$TemplateParameters = @{
    FunctionApp_Name = $FunctionApp_Name;
    StorageAccount_Name = $StorageAccount_Name;
    DataFactory_Name = $DataFactory_Name
}

$output = (New-AzResourceGroupDeployment `
    -ResourceGroupName $resourcegroupname `
    -TemplateFile ./deploy.json `
    -Mode Incremental `
    -TemplateParameterObject $TemplateParameters)

if ($output.ProvisioningState -ne "Succeeded") { 
    echo "Deploying app resources failed with message: "
    $output
    exit 
}

echo "Creating fileshare in " $StorageAccount_Name

$accountKeys = (Get-AzStorageAccountKey -ResourceGroupName $resourcegroupname -AccountName $StorageAccount_Name)
$storageContext = New-AzStorageContext $StorageAccount_Name  $accountKeys[0].Value

$fileshareName = "filedrop"

New-AzStorageShare -Name $fileshareName -Context $storageContext

echo "Uploading data to storage account " $StorageAccount_Name

Set-AzStorageFileContent -ShareName $fileshareName -Source ".\TestData1.tar" -Context $storageContext
Set-AzStorageFileContent -ShareName $fileshareName -Source ".\TestData2.tar" -Context $storageContext

echo "Creating local settings file for development"

$settingsFileTemplate = Get-Content .\local.settings.json.template
$settingsFileTemplate `
    -replace "{fileshareName}", $fileshareName `
    -replace "{storageAccountName}", $StorageAccount_Name `
    -replace "{storageAccountKey}", $accountKeys[0].Value `
    | Set-Content .\local.settings.json

copy .\local.settings.json ..\src\ExtractFunction -Force




