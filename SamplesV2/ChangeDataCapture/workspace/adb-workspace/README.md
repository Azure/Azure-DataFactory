# Azure Databricks Workspace ARM Template

## Parameters

| Name | Type | Required | Description |
| :------------- | :----------: | :----------: | :------------- |
| Deployment Tla | string | Yes | The three letter acronym to make the storage account unique when combined with the uniqueString and associated with the workspace|
| Deployment Type | string | Yes | devtest, acceler, prod, shared |
| Location | string | No | Specifies the Location based on the Region selected above |
| Sql Administrator Login | string | Yes | The username of the SQL Administrator | 
| Sql Administrator Login Password | string | Yes | The password for the SQL Administrator |

## Outputs

| Name | Type | Description |
| :------------- | :----------: | :------------- |
| sqlServerName | string | Specifies the name of the Azure SQL Database Server |
| sqlDatabaseName | string | Specifies the name of the Azure SQL Database |
| sqlAdministratorLogin | string | The SQL Admin Login|
| sqlConnectionString | string | The SQL Connection String |
| databricksOverviewUrl | string | Databricks Workspace Overview URL |
