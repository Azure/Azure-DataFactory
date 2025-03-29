param batchAccountName string = '${uniqueString(resourceGroup().id)}batch'
param location string = resourceGroup().location
param batchVMSku string
param batchPoolName string
param saName string

@allowed([
  'BatchService'
  'UserSubscription'
])
param allocationMode string = 'BatchService'

resource sa 'Microsoft.Storage/storageAccounts@2021-06-01' existing = {
  name: saName
}

var connStr = 'DefaultEndpointsProtocol=https;AccountName=${sa.name};AccountKey=${sa.listKeys().keys[0].value}'

resource batchAccount 'Microsoft.Batch/batchAccounts@2020-09-01' = {
  name: batchAccountName
  location: location
  properties: {
    poolAllocationMode: allocationMode
  }
}

resource pool 'Microsoft.Batch/batchAccounts/pools@2021-06-01' = {
  name: batchPoolName
  parent: batchAccount
  properties: {
    deploymentConfiguration: {
      virtualMachineConfiguration: {
        imageReference: {
          offer: 'ubuntu-server-container'
          publisher: 'microsoft-azure-batch'
          sku: '20-04-lts'
          version: 'latest'
        }        
        nodeAgentSkuId: 'batch.node.ubuntu 20.04'
      }            
    }
    scaleSettings: {
      fixedScale: {
        targetDedicatedNodes: 1   
      }
    }
    startTask: {
      commandLine: '/bin/bash -c "apt-get update;apt-get install -y python3-pip;pip3 install azure-storage-blob==12.8.1;pip3 install aiofile==3.5.0;pip3 install aiohttp==3.7.4.post0;pip3 install azure-eventhub==5.5.0;export CONN_STR=${connStr}"'
      maxTaskRetryCount: 2
      userIdentity: {
        autoUser: {
          elevationLevel: 'Admin'
          scope: 'Pool'
        }
      }
      waitForSuccess: true
    }
  vmSize: batchVMSku
  }
}

output batchName string = batchAccount.name
