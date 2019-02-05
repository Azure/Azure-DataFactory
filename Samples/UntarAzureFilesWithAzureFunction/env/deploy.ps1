param([String]$resourcegroupname="dffunctionssample")

if ($resourcegroupname.Length -gt 17)
{
    echo "Please provide a resource group name with 17 or less characters."
    exit
}

echo "Creating resource group with name " $resourcegroupname

New-AzResourceGroup -Name $resourcegroupname -Location "West US"

echo "Deploying app resources"

$FunctionApp_Name = $resourcegroupname + "app"
$StorageAccount_Name = $resourcegroupname + "storage"
$DataFactory_Name = $resourcegroupname + "factory"

$output = (New-AzResourceGroupDeployment `
    -ResourceGroupName $resourcegroupname `
    -TemplateFile ./deploy.json `
    -Mode Incremental `
    -FunctionApp_Name $FunctionApp_Name `
    -StorageAccount_Name $StorageAccount_Name `
    -DataFactory_Name $DataFactory_Name)

if ($output.ProvisioningState -ne "Succeeded") { 
    echo "Deploying app resources failed."
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




