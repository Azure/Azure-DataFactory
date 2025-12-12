# Azure Analytics Accelerator

The following accelerator can be used to deploy `Azure Data Factory`, `Azure Databricks`, `Azure Data Lake Storage`, and `Azure SQL Database` into an Azure Resource Group.  It will allow you to explore some of the Data Integration, Data Lake, and Data Lakehouse capabilities available on Microsoft Azure.  

## Purpose

The purpose of this Analytics Accelerator is to help you learn and grow through Hands-on common use cases that show you how to use things like ADF pipelines, Databricks notebooks, and SQL scripts.

This [GitHub Sample](https://github.com/Azure/Azure-DataFactory/tree/main/SamplesV2/ChangeDataCapture) along with an Azure Subscription [No Azure Subscription click here](https://azure.microsoft.com/en-us/free/) should allow you to accelerate:

* Business Value
* Time-to-insight
* Modernization
* Skilling
* Proof of Concepts
* Architecture choice
* Infrastructure as code for deployment via ARM template

## Use Case

| Deployment | Use Case Name | Use Case Type | Dataset | Description | Code | Instruction Steps |
| :------------- | :----------: | :----------: | :----------: | :----------: | :----------: | :----------: |
| [Azure Data Factory, Azure Databricks, and Azure SQL Database](https://github.com/Azure/Azure-DataFactory/tree/main/SamplesV2/ChangeDataCapture#deploy-azure-datafactory-azure-databricks-and-azure-sql-database) | Change Data Capture | Azure Data Factory, Azure Databricks, Azure Data Lake Storage, Azure SQL Database | AdventureworksLT | Change Data Capture using ADF and Databricks Autoloader | [Code](https://github.com/Azure/Azure-DataFactory/tree/main/SamplesV2/ChangeDataCapture/usecases/cdc/code) | [Steps](https://github.com/Azure/Azure-DataFactory/tree/main/SamplesV2/ChangeDataCapture/usecases/cdc/steps/usecasesteps.md) |

## Prerequisites

- Owner to the Azure Subscription being deployed. This is for creation of a separate Analytics Accelerator Resource Group and to delegate roles necessary for this deployment.

## Deploy Azure Data Factory, Azure Databricks, ADLS, and Azure SQL Database

`Together with Azure Data Factory, Azure Databricks, Azure Data Lake Storage Gen2, and Azure SQL Database`

[![Deploy To Azure](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/deploytoazure.svg?sanitize=true)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2FAzure-DataFactory%2Fmain%2FSamplesV2%2FChangeDataCapture%2Fworkspace%2Fadb-workspace%2Fazuredeploy.json) [![Visualize](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.svg?sanitize=true)](http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2FAzure-DataFactory%2Fmain%2FSamplesV2%2FChangeDataCapture%2Fworkspace%2Fadb-workspace%2Fazuredeploy.json)

This template deploys the following:

- Azure Data Factory
- Azure Databricks Workspace
- Azure Data Lake Storage Gen2
- Azure SQL Database

## Post Deployment Steps

One you have complete the deployment please go to [Next Steps](https://github.com/Azure/Azure-DataFactory/tree/main/SamplesV2/ChangeDataCapture/usecases/cdc/steps/usecasesteps.md) to configure and run the Change Data Capture of changes made to the Azure SQL Database which ADF copies and Azure Databricks autoloads.