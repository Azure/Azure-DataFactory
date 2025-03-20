# ADF pipeline with Azure Batch 

Azure Batch is a service that provides great elasticity and scalability. Azure Data Factory is the ETL service in Azure. Using ADF pipeline, we can easily trigger the Batch job to do various  processing tasks in a cost efficient manner, which is extremely powerful when there are huge number of files need to be processed, e.g. decryption etc.

## Introduction

In this solution, we simulate a scenario to use ADF pipeline to trigger Azure Batch job for file decompression. 

There is bicep scripts and powershell scripts for deploying all necesary Azure resources, i.e. ADF, storage account, Batch account etc. Source code for file decompression in python is included. There are also some sample data

**_NOTE:_** for simplicity, resouces are deployed with public access. In production environment, private endpoint for relevant resources should be used. 

## How to deploy

As Azure resources is deployed using Bicep, Bicep tools need to be installed following this [guide](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/install).

After installation, run below scripts to deploy all resources. 

```bash
$ az login
$ az account set --subscription <your subscription>
$ az deployment sub create -f main.bicep -l northeurope
```

## How to run

Get storage account connection string
![Storage Account connection string](https://raw.githubusercontent.com/zhenbzha/Azure-DataFactory/sample-azbatch/SamplesV2/AzureBatchIntegration/images/StorageAccountCconnectionString.png)

Trigger pipeline in ADF and paste the connection string to parameter
![ADF pipeline parameter](https://raw.githubusercontent.com/zhenbzha/Azure-DataFactory/sample-azbatch/SamplesV2/AzureBatchIntegration/images/PipelineParameter.png)

