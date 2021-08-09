# Azure Analytics Accelerator

The following accelerator can be used to deploy `Azure Databricks`, `Azure Data Factory`, `Azure Data Lake Storage`, and `Azure SQL Database` into an Azure Resource Groups  It will allow you to explore some of the Data Integration, Data Lake, and Data Lakehouse capabilities available on Microsoft Azure.  

## Purpose

The purpose of this Analytics Accelerator is to help you learn and grow through Hands-on common use cases that show you how to use things like ADF pipelines, Databricks notebooks, and SQL scripts.

This [GitHub Repository](https://github.com/DataSnowman/ChangeDataCapture) along with an Azure Subscription [No Azure Subscription click here](https://azure.microsoft.com/en-us/free/) should allow you to accelerate:

* Business Value
* Time-to-insight
* Modernization
* Skilling
* Proof of Concepts
* Architecture choice
* Infrastructure as code for PoC, Dev, Test, Prod

## Use Case

| Deployment | Use Case Name | Use Case Type | Dataset | Description | Code | Instruction Steps |
| :------------- | :----------: | :----------: | :----------: | :----------: | :----------: | :----------: |
| [Azure Data Factory, Azure Databricks, and Azure SQL Database](https://github.com/DataSnowman/ChangeDataCapture#deploy-azure-datafactory-azure-databricks-and-azure-sql-database) | Change Data Capture | Azure Databricks, ADF, Azure SQL DB | AdventureworksLT | Change Data Capture using ADF and Databricks Autoloader | [Code](https://github.com/DataSnowman/ChangeDataCapture/tree/main/usecases/cdc/code) | [Steps](https://github.com/DataSnowman/ChangeDataCapture/blob/main/usecases/cdc/steps/usecasesteps.md) |

## Prerequisites

- Owner to the Azure Subscription being deployed. This is for creation of a separate Analytics Accelerator Resource Group and to delegate roles necessary for this deployment.

## Deploy Azure Datafactory, Azure Databricks, and Azure SQL Database

`Together with Azure Databricks, Azure Data Lake Storage Gen2, Azure Data Factory, and Azure SQL Database`

[![Deploy To Azure](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/deploytoazure.svg?sanitize=true)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FDataSnowman%2FChangeDataCapture%2Fmain%2Fworkspace%2Fadb-workspace%2Fazuredeploy.json) [![Visualize](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.svg?sanitize=true)](http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2FDataSnowman%2FChangeDataCapture%2Fmain%2Fworkspace%2Fadb-workspace%2Fazuredeploy.json)

This template deploys the following:

- Azure Databricks Workspace
- Azure Data Lake Storage Gen2
- Azure Data Factory
- Azure SQL Database
