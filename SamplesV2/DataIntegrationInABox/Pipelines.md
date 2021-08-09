# Data Integration Pipelines 

The Data Factory that you can deploy with this solution will contain several pipelines that showcase different activities.

## Pipelines 

### Top level pipeline 

- ``00-ControlPipeline`` This pipeline calls all other pipelines and serves as orchestration for the entire loading process. This pipeline is triggered by a daily trigger. It uses Execute Pipeline activities to call the other pipelines. 

    ![00-ControlPipeline](images/00-ControlPipeline.png?raw=true)

### Folder D365

This folder contains the pipelines for handling data from Dynamics 365. If you have created a demo account and entered its credentials when deploying the solution to Azure, the pipelines in this folder will process the data from Dynamics 365. 

- ``01-CopySalesActivityToCosmos`` This pipeline reads the sales activity entity from Dynamics 365 and writes its contents to Cosmos DB. It uses a copy activity to achieve this

    ![01-CopySalesActivityToCosmos](images/01-CopySalesActivityToCosmos.png?raw=true)
    
    After this pipeline was run, you can browse your Cosmos DB using the data explorer from the Azure portal to see the entities that were written

    ![CosmosDB-SalesActivities](images/CosmosDB-SalesActivities.png?raw=true)

- ``02-CopyD365SalesLeadToStorage`` This pipeline copies the Leads from Dynamics 365 to your storage account. It uses a copy activity to achieve this

    ![02-CopyD365SalesLeadToStorage](images/02-CopyD365SalesLeadToStorage.png?raw=true)

    After it was run, you can locate the data in your storage accout when browsing it through the Azure portal. The leads will be contained in the ``d365data`` container in a folder structure containing the year, month and day, you ran the pipeline in a file called ``leads.txt`

    ![StorageAccountLeads](images/StorageAccountLeads.png?raw=true)

- ``03-CopyD365SalesLeadsToSQL`` This pipeline copies the same leads to the dedicated pool of your Azure Synapse Analytics workspace. You need to grant permissions to your ADF managed identity in order to run this pipeline by following the steps in the [main documentation](README.md).

    ![03-CopyD365SalesLeadsToSQL](images/03-CopyD365SalesLeadsToSQL.png?raw=true)

    After you ran this pipeline, your sales leads are visible in the dedicated SQL pool in your Synapse workspace

    ![SynapseData](images/SynapseData.png?raw=true)

- ``04-AnonymizeSalesLeads`` This pipelines runs the Leads through a [presidio](https://github.com/microsoft/presidio) webapp in order to remove personal information from it before storing it to the data lake. It iterates the files in the storage account and runs each row of each file through the presidio analyzer and presidio anonymizer before storing them to the storage account. Calling the presidio solution is handled in ``04-01-DataAnonymizationSingleFile`` for each single file. 

    First all files are iterated

    ![04-AnonymizeSalesLeads](images/04-AnonymizeSalesLeads.png?raw=true)

    For each file the ``04-01-DataAnonymizationSingleFile`` is called

    ![04-AnonymizeSalesLeadsForeach](images/04-AnonymizeSalesLeadsForeach.png?raw=true)

    In ``04-01-DataAnonymizationSingleFile``, the files are read and a loop over their contents is created

    ![04-01-DataAnonymizationSingleFile](images/04-01-DataAnonymizationSingleFile.png?raw=true)

    In the foreach loop, the presidio APIs are called:

    ![04-01-DataAnonymizationSingleFileForeach](images/04-01-DataAnonymizationSingleFileForeach.png?raw=true)

    After ``04-AnonymizeSalesLeads`` has been called, in the storage account in the ``d365data`` container, there will be a folder called ``leads_anonymized`` that will contain the anonymized records

    ![AnonymizedData](images/AnonymizedData.png?raw=true)

- ``05-TransformDynamicsData`` calls the ``DFActivitiesAndLeads`` dataflow (description below) that transforms the dynamics data and loads it into [Common Data Model](https://docs.microsoft.com/en-us/common-data-model/) output format

    ![05-TransformDynamicsData](images/05-TransformDynamicsData.png?raw=true)

### Folder OpenDataset

This folder contains Pipelines to load and transform data from the New York Taxi dataset [hosted on Azure](https://docs.microsoft.com/de-de/azure/open-datasets/dataset-taxi-yellow?tabs=azureml-opendatasets)

- ``01-StageOpenData`` copies the Taxi Data from the Parquet files in which it is provied into json files. It uses a copy activity to move the data

    ![01-StageOpenData](images/01-StageOpenData.png?raw=true)

- ``02-TransformTaxiData`` calls the ``DFMoveTaxiData`` dataflow (described below) that transforms the taxi data before loading it to the Synapse Analytics dedicated SQL pool

    ![02-TransformTaxiData](images/02-TransformTaxiData.png?raw=true)

## Dataflows

The solution contains two dataflows that transform the data:

- ``DFActivitiesAndLeads`` This dataflow joins the data from the Dynamics 365 Activites (located in Cosmos DB) and the Dynamics 365 Leads (located in the Azure Synapse Dedicated SQL Pool). It then checks if an activity is associated with a lead or not and writes the activitites with leads and the activities without leads into different folders

    ![DFActivitiesAndLeads](images/DFActivitiesAndLeads.png?raw=true)

    Running this dataflow for the first time will take several minutes as the cluster hosting the dataflow needs to be started. After the dataflow has completed, you will find two folders in the ``exports`` container of your storage account, one containing the activities with leads the other one containing the activities without leads

    ![ActivitiesCDMExport](images/ActivitiesCDMExport.png?raw=true)

- ``DFMoveTaxiData`` loads the taxi data and adds a qualifier about the trip distance as a derived column to the dataset. Then it stores the data in your Azure Synapse Dedicated SQL Pool.

    ![DFMoveTaxiData](images/DFMoveTaxiData.png?raw=true)

    After running this dataflow, you will find a taxidata table in your dedicated synapse pool containg the original data and the distance qualification from the derived column in the dataflow. 

    ![TaxidataTransformed](images/TaxidataTransformed.png?raw=true)