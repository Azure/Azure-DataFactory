param storageAccountName string = '${uniqueString(resourceGroup().id)}sa'
param location string = resourceGroup().location
param containerNames array = [
  'raw'
  'curated'
  'azure-batch'
]

resource sa 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
  }
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2019-06-01' = [for name in containerNames: {
  name: '${sa.name}/default/${name}'
}]

output saName string = sa.name
