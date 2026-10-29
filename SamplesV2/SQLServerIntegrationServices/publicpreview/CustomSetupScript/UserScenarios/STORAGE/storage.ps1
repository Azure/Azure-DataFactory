# Create a storage context
$ctx = New-AzureStorageContext YourStorageAccountName YourStorageAccountKey

# Create a container
$containerName = "newcontainer"
New-AzureStorageContainer $containerName -Context $ctx -Permission blob

# Delete a container
#Remove-AzureStorageContainer -Container $containerName -Context $ctx