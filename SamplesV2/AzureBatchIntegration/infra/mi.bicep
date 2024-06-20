param managedIdentityName string = '${uniqueString(resourceGroup().id)}-mi'
param location string = resourceGroup().location
param saName string

var contributorRoleDefinitionId = 'b24988ac-6180-42a0-ab88-20f7382dd24c' // Contributor role
var SBDCRoleDefinitionId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe' // Storage Blob Data Contributor

resource sa 'Microsoft.Storage/storageAccounts@2021-06-01' existing = {
  name: saName
}

resource mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: managedIdentityName
  location: location
}

resource roleassignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(contributorRoleDefinitionId, resourceGroup().id)

  properties: {
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleDefinitionId)
    principalId: mi.properties.principalId
  }
}

resource roleassignment2 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(SBDCRoleDefinitionId, resourceGroup().id)
  scope: sa
  properties: {
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', SBDCRoleDefinitionId)
    principalId: mi.properties.principalId
  }
}

output miName string = mi.name
output miId string = mi.properties.principalId
