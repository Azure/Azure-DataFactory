param location string = resourceGroup().location
param miName string
param saName string
param repoURL string

var uaMI = '${resourceGroup().id}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/${miName}'

resource sa 'Microsoft.Storage/storageAccounts@2021-06-01' existing = {
  name: saName
}

resource deploymentScript 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'deploymentScript'
  location: location
  kind: 'AzurePowerShell'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uaMI}': {}
    }
  }
  properties: {
    azPowerShellVersion: '3.0'
    scriptContent: loadTextContent('dscript.ps1')
    arguments: '-StorageAccountName ${saName} -RepoURL ${repoURL}'
    retentionInterval: 'P1D'
    timeout: 'P1D'
    cleanupPreference: 'OnSuccess'
  }
}
