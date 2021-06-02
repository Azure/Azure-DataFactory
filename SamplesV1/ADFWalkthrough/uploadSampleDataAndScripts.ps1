<#
.SYNOPSIS 
uploadSampleDataAndScripts.ps1 uploads the sample data and script to your Azure Storage, and Azure SQL Database.

.DESCRIPTION
Remember to update the storage account name, storage account key, and information for
Azure SQL Database (Server, User and password) before running the script.

.NOTES 
File Name : uploadSampleDataAndScripts.ps1
Version   : 2.0
#>    

$storageAccount = "<storage account>"
$storageKey = "<storage account key>"
$azuresqlServer = "<sql azure server>.database.windows.net"
$azuresqlUser = "<sql azure user>"
$azuresqlPassword = "<sql azure password>"

#Import-Module Azure -Verbose:$false

$destContext = New-AzureStorageContext  –StorageAccountName $storageAccount -StorageAccountKey $storageKey -ea silentlycontinue
If ($destContext -eq $Null) {
	Write-Host "Invalid storage account name and/or storage key provided"
	Exit
}

$adfcontainerName = "adfwalkthrough"

Write-Verbose   "Preparing the storage account. Adding the container [$adfcontainerName]"
$destContext = New-AzureStorageContext  –StorageAccountName $storageAccount -StorageAccountKey $storageKey -ea silentlycontinue
If ($destContext -eq $Null) {
	Write-Verbose "Invalid storage account name and/or storage key provided"
	Exit
}

#check whether the Azure storage containe already exists
$container = Get-AzureStorageContainer -Name $adfcontainerName -context $destContext –ea silentlycontinue
If ($container -eq $Null) {
	Write-Host "Creating storage container [$adfcontainerName]"
	New-AzureStorageContainer -Name $adfcontainerName -context $destContext 
}
else {
	Write-Host "[$adfcontainerName] exists."
}

# STEP 1- Upload sample data and script files to the blob container
Write-Host  "Uploading sample data and script files to the storage container [$adfcontainerName]"
Set-AzureStorageBlobContent -File ".\Sampledata\rawstats-20140501.csv" -Container $adfcontainerName -Context $destContext -Blob "logs/rawgameevents/raw1.csv" -Force
Set-AzureStorageBlobContent -File ".\Sampledata\rawstats-20140502.csv" -Container $adfcontainerName -Context $destContext -Blob "logs/rawgameevents/raw2.csv" -Force
Set-AzureStorageBlobContent -File ".\Sampledata\rawstats-20140503.csv" -Container $adfcontainerName -Context $destContext -Blob "logs/rawgameevents/raw3.csv" -Force
Set-AzureStorageBlobContent -File ".\Sampledata\rawstats-20140504.csv" -Container $adfcontainerName -Context $destContext -Blob "logs/rawgameevents/raw4.csv" -Force
Set-AzureStorageBlobContent -File ".\Sampledata\refgeocodedictionary.csv" -Container $adfcontainerName -Context $destContext -Blob "refdata/refgeocodedictionary/refgeocodedictionary.csv" -Force
Set-AzureStorageBlobContent -File ".\Sampledata\refmarketingcampaign.csv" -Container $adfcontainerName -Context $destContext -Blob "refdata/refmarketingcampaign/refmarketingcampaign.csv" -Force
Set-AzureStorageBlobContent -File ".\Scripts\partitionlogs.hql" -Container $adfcontainerName -Context $destContext -Blob "scripts/partitionlogs.hql" -Force
Set-AzureStorageBlobContent -File ".\Scripts\enrichlogs.pig" -Container $adfcontainerName -Context $destContext -Blob "scripts/enrichlogs.pig" -Force
Set-AzureStorageBlobContent -File ".\Scripts\transformdata.hql" -Container $adfcontainerName -Context $destContext -Blob "scripts/transformdata.hql" -Force

# STEP 2
sqlcmd -S $azuresqlServer -U $azuresqlUser -P $azuresqlPassword -Q "create database MarketingCampaigns"
sqlcmd -S $azuresqlServer -U $azuresqlUser -P $azuresqlPassword -d MarketingCampaigns -i ".\Scripts\MarketingCampaignEffectiveness.sql"

# STEP 3- You are all set!
Write-Host  -foreground green (Get-Date)  "Summary"
Write-Host  -foreground green (Get-Date)  "1. Uploaded Sample Data Files to blob container."
Write-Host  -foreground green (Get-Date)  "2. Uploaded Sample Script Files to blob container."
Write-Host  -foreground green (Get-Date)  "3. Created 'MarketingCampaigns' Azure SQL database and tables."
Write-Host  -foreground green (Get-Date)  "You are ready to deploy Linked Services, Tables and Pipelines."
