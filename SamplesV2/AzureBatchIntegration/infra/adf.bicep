param dataFactoryName string = '${uniqueString(resourceGroup().id)}-adf'
param location string = resourceGroup().location
param saName string
param batchName string
param batchPoolName string

var storageAccountLinkedServiceName = 'ls-sa'
var batchAccountLinkedServiceName = 'ls-batch'
var pipelineName = 'AzureBatchPipeline'
var batchUri = 'https://${batchName}.${location}.batch.azure.com'

resource sa 'Microsoft.Storage/storageAccounts@2021-06-01' existing = {
  name: saName
}

resource batch 'Microsoft.Batch/batchAccounts@2021-06-01' existing = {
  name: batchName
}

resource dataFactory 'Microsoft.DataFactory/factories@2018-06-01' = {
  name: dataFactoryName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
}

resource storageAccountLinkedService 'Microsoft.DataFactory/factories/linkedservices@2018-06-01' = {
  parent: dataFactory
  name: storageAccountLinkedServiceName
  properties: {
    type: 'AzureBlobStorage'
    typeProperties: {
      connectionString: 'DefaultEndpointsProtocol=https;AccountName=${sa.name};AccountKey=${sa.listKeys().keys[0].value}'
    }
  }
}

resource batchLinkedService 'Microsoft.DataFactory/factories/linkedservices@2018-06-01' = {
  parent: dataFactory
  name: batchAccountLinkedServiceName
  properties: {
    type: 'AzureBatch'
    typeProperties: {
      batchUri: batchUri      
      accessKey: {
        type: 'SecureString'
        value: batch.listKeys().primary
      }
      linkedServiceName: {
        referenceName: storageAccountLinkedService.name
        type: 'LinkedServiceReference'
      }
      poolName: batchPoolName
      accountName: batchName
    }
  }
}

resource dataFactoryPipeline 'Microsoft.DataFactory/factories/pipelines@2018-06-01' = {
  parent: dataFactory
  name: pipelineName  
  properties: {
    parameters: {
      'CONN_STR': {
        type: 'String'        
      }
    }
    activities: [
      any({
        
        name: 'Process'
        type: 'Custom'   
        linkedServiceName: {
          referenceName: batchLinkedService.name
          type: 'LinkedServiceReference'
        }
        typeProperties: {                
          command: '@concat(\'python3 adf_main.py \', pipeline().parameters.CONN_STR)'
          folderPath: 'azure-batch'

          resourceLinkedService: {
            parameters: {}
            referenceName: storageAccountLinkedService.name
            type: 'LinkedServiceReference'
          }
        }
      })
    ]
  }
}
