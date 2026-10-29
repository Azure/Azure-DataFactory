targetScope = 'subscription'

var rgName = 'decompdemo9-rg'
var batchVMSku = 'Standard_D2s_v3'
var batchPoolName = '${batchVMSku}-pool'

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: rgName
  location: deployment().location
}

module saDeploy 'storage.bicep' = {
  scope: rg
  name: 'saDeploy'
}

module batchDeploy 'batch.bicep' = {
  scope: rg
  name: 'batchDeploy'
  params: {  
    saName: saDeploy.outputs.saName
    batchVMSku: batchVMSku
    batchPoolName: batchPoolName
  }
}

module miDeploy 'mi.bicep' = {
  scope: rg
  name: 'miDeploy'
  params: {
    saName: saDeploy.outputs.saName
  }
}

module adfDeploy 'adf.bicep' = {
  scope: rg
  name: 'adfDeploy'
  params: {
    batchName: batchDeploy.outputs.batchName  
    batchPoolName: batchPoolName
    saName: saDeploy.outputs.saName
  }
}

module scriptDeploy 'deploymentscript.bicep' = {
  scope: rg
  name: 'scriptDeploy'
  params: {
    miName: miDeploy.outputs.miName
    saName: saDeploy.outputs.saName
    repoURL: 'https://raw.githubusercontent.com/zhenbzha/Azure-DataFactory/sample-azbatch/SamplesV2/AzureBatchIntegration'
  }
}
