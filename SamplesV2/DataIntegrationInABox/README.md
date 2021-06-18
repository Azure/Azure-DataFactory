# Data integration in a box

This repository will deploy an Azure Data Factory and associated resources to your Azure subscription. You will need to have an active Azure subscription to run this sample. 

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fbenkettner%2FADFDemoDeploy%2Fmain%2Ftemplate.json)


## What's contained

After deployment, your resource group will contain: 

* an Azure data factory 
* a Cosmos DB account
* a synapse analytics workspace with a dedicated SQL pool
* a storage account

The data factory contains 

* A pipeline that copies parquet data from an open data source to Synapse 
* 2 pipelines that copy data from a Dynamics 365 demo account to Cosmos DB and Synapse
* A mapping dataflow that transforms data from these two data sources and loads it to a CDM data structure
* A pipeline that runs the mapping dataflow

For details on the pipelines view [this](Pipelines.md) document. 

## Before you start

This demo has a few prerequesites that you will have to take care of.

### Pre-Deployment: create D365 account
You will need a (free demo) Dynamics 365 account and the credentials to that account. You can obtain an account on [https://trials.dynamics.com](https://trials.dynamics.com/).

For deploying this template you will need the username and password for a user registered with that demo account as well as the demo accounts URL which will typically be https://\<your-tenant-name\>.crm8..dynamics.com.

### Post-Deployment: grant acces to Synapse dedicated pool for ADF
Furthermore you will need to grant your data factory permisssion to access your Synapse dedicated SQL pool. 

You can do this by running the following SQL after logging into your dedicated SQL pool via the Azure Synapse workspace:

```sql
create user [<your_ADF_name_here>] from external provider;
exec sp_addrolemember 'db_owner', '<your_ADF_name_here>';
```

To do so, execute the follwing steps: 

1. Navigate to your resource group in the Azure portal, then open your Synapse workspace 

    ![Open Synapse Overview](images/sql-dedicated-pool-permissions.-01.png?raw=true)

2. On the overview page of your Synapse Workspace click the tile to open the synapse workspace 

    ![Open Synapse Workspace](images/sql-dedicated-pool-permissions.-02.png?raw=true)

3. In the left side menu chose "develop" to open the development blade: 

    ![Open develpment pane](images/sql-dedicated-pool-permissions.-03.png?raw=true)

4. In the development blade, click the plus symbol and then click "SQL script" to start writing a new SQL script: 

    ![Start new SQL Script](images/sql-dedicated-pool-permissions.-04.png?raw=true)

5. At the top of the editor, use the "Connect to" dropdown to connect to your dedicated SQL pool 

    ![Open Synapse Workspace](images/sql-dedicated-pool-permissions.-05.png?raw=true)

6. Make sure you have also selected the dedicated pool database: 

    ![Open Synapse Workspace](images/sql-dedicated-pool-permissions.-06.png?raw=true)

7. Paste your code and click "Run" at the top of the editor window to execute it. After a few seconds you will see a green checkmark and a success message at the bottom of the window. 