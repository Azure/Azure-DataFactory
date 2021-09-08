$tenant_id = <your tenant id>                               ## Replace with your tenant id
$subscription_id = <your subscription id>                   ## Replace with your subscription id
$workspace_name = "twitterhashtagsandbox"                   ## Replace with your Synapse workspace name. Default "twitterhashtagsandbox".
$primary_storage_workspace =  $workspace_name               ## Replace with your Synapse workspace Primary ADLS Gen2 accoount. Default is same as workspace name.
$primary_file_system_name = "workspace"                     ## Replace with your Synapse workspace Primary ADLS Gen2 file system. Default "workspace".
$KeyVault_baseUrl = <your Key Vault base Uri>               ## Repace with your Key Vault base Uri. 
$hashtag = "#AzureDataFactory"                              ## Replace with your hashtag

$sparkpool_name = "mySparkPool"
$notebook_name = "myNotebook"   ## DO NOT change the name, refrenced in pipeline definition

Connect-AzAccount -Tenant  $tenant_id -Subscription $subscription_id
$context = Get-AzContext

Write-Host "----- Create SynapseSparkPool: workspace($workspace_name) , sparkpool($sparkpool_name) " -ForegroundColor White
New-AzSynapseSparkPool -WorkspaceName $workspace_name -Name $sparkpool_name `
    -AutoScaleMinNodeCount 3 -AutoScaleMaxNodeCount 3 -SparkVersion 2.4 -NodeSize Small `
    -EnableAutoPause -AutoPauseDelayInMinute 15

Write-Host "-----SynapseSparkPool: workspace($workspace_name) , sparkpool($sparkpool_name) apply package setting. It will take a while" -ForegroundColor White
Update-AzSynapseSparkPool -WorkspaceName $workspace_name -Name $sparkpool_name `
    -LibraryRequirementsFilePath ".\requirements.txt"

Write-Host "-----Set-AzSynapseNotebook: $notebook_name" -ForegroundColor White    
Set-AzSynapseNotebook -WorkspaceName $workspace_name -Name $notebook_name `
    -DefinitionFile ".\myNotebook.ipynb" 
Set-AzSynapseNotebook -WorkspaceName $workspace_name -Name $notebook_name `
    -DefinitionFile ".\myNotebook.ipynb" -SparkPoolName $sparkpool_name -ExecutorCount 1 ### Sparkpool attach is not su

## Replace workspace primary storage name in dataset
$JsonFilePath_in = '.\input_json\adlsBinary.json'
$JsonFilePath_out = '.\output_json\adlsBinary.json'

Write-Host "-----Dateset JsonFilePath_in: $JsonFilePath_in" -ForegroundColor White
Write-Host "-----Dataset JsonFilePath_out: $JsonFilePath_out" -ForegroundColor White

$JsonData = Get-Content $JsonFilePath_in -raw | ConvertFrom-Json

$JsonData.update | % { if($JsonData.properties.linkedServiceName.referenceName)
    {
        $JsonData.properties.linkedServiceName.referenceName = $workspace_name + "-WorkspaceDefaultStorage"
    }
}

$JsonData | ConvertTo-Json -Depth 4  | set-content $JsonFilePath_out 

## Replace key vault baseUrl in Linked Service

$JsonFilePath_in = '.\input_json\mylsKeyVault.json'
$JsonFilePath_out = '.\output_json\mylsKeyVault.json'

Write-Host "-----Key Vault Linked Service JsonFilePath_in: $JsonFilePath_in" -ForegroundColor White
Write-Host "-----Key Vault Linked Service JsonFilePath_out: $JsonFilePath_out" -ForegroundColor White

$JsonData = Get-Content $JsonFilePath_in -raw | ConvertFrom-Json

$JsonData.update | % { if($JsonData.properties.typeProperties.baseUrl)
    {
        $JsonData.properties.typeProperties.baseUrl = $KeyVault_baseUrl
    }
}

$JsonData | ConvertTo-Json -Depth 4  | set-content $JsonFilePath_out 

## Replace hashtag in pipeline notebook activity parameter

$JsonFilePath_in = '.\input_json\myPipeline.json'
$JsonFilePath_out = '.\output_json\myPipeline.json'

Write-Host "-----pipeline JsonFilePath_in: $JsonFilePath_in" -ForegroundColor White
Write-Host "-----pipeline JsonFilePath_out: $JsonFilePath_out" -ForegroundColor White

$JsonData = Get-Content $JsonFilePath_in -raw | ConvertFrom-Json -Depth 9

$JsonData.update | % { if($JsonData.properties.parameters)
    {
        $JsonData.properties.parameters.account_name.defaultValue = $primary_storage_workspace
        $JsonData.properties.parameters.container_name.defaultValue = $primary_file_system_name
        $JsonData.properties.parameters.hashtag.defaultValue = $hashtag
    }
}

$JsonData | ConvertTo-Json -Depth 9  | set-content $JsonFilePath_out 

## Create artifacts in workspace

$linkedService1 = "mylsKeyVault"
$datasetname1 = "adlsBinary"
$piplinename1 = "myPipeline"

Write-Host "-----Set-AzSynapseLinkedService: Azure Key Vault---" -ForegroundColor White
Set-AzSynapseLinkedService -WorkspaceName $workspace_name `
    -Name $linkedService1 -DefinitionFile ".\output_json\mylsKeyVault.json" -DefaultProfile $context

Write-Host "-----Set-AzSynapseDataset: ADLS Gen2---" -ForegroundColor White
Set-AzSynapseDataset -WorkspaceName $workspace_name -Name $datasetname1 -DefinitionFile ".\output_json\adlsBinary.json" -DefaultProfile $context

Write-Host "-----Set-AzSynapsePipeline---" -ForegroundColor White
Set-AzSynapsePipeline -WorkspaceName $workspace_name -Name $piplinename1 -DefinitionFile ".\output_json\myPipeline.json" -DefaultProfile $context