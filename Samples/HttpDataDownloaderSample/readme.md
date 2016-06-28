# HttpDataDownloaderSample

## Introduction

This showcases downloading of data from an HTTP Endpoint to Azure Blob Storage using 
[Azure Data Factory Custom C# Activity](http://azure.microsoft.com/en-us/documentation/articles/data-factory-use-custom-activities/). Once the data is downloaded into Azure Blob, it can be consumed for further processing.

## Contents

* /CustomDotNetActivity - Contains custom activities 
* /LinkedServices - Contains linked services definitions
* /Pipelines - Contains pipeline definition
* /Tables - Contains datasets/table definitions

## Getting Started

### Azure Data Factory

1. `New-AzureRmResourceGroup -Name [RESOURCE_GROUP_NAME]  -Location "West US"`
2. `New-AzureRmDataFactory -ResourceGroupName [RESOURCE_GROUP_NAME] -Name [DATA_FACTORY_NAME] –Location "West US"`

### Linked Services

1. `New-AzureRmDataFactoryLinkedService -ResourceGroupName [RESOURCE_GROUP_NAME] -DataFactoryName [DATA_FACTORY_NAME] -File /LinkedServices/RawEventsLinkedService.json`

Optionally, if you choose to run your own instance of HDInsight in lieu of the on-demand HDInsight cluster:

1. `New-AzureRmDataFactoryLinkedService -ResourceGroupName [RESOURCE_GROUP_NAME] -DataFactoryName [DATA_FACTORY_NAME] -File /LinkedServices/HDInsightLinkedService.json`


### Dataset

1. `New-AzureRmDataFactoryDataset -ResourceGroupName [RESOURCE_GROUP_NAME] -DataFactoryName [DATA_FACTORY_NAME] -File /Tables/RawEventsTable.json`

### Custom Activity

Given each time splice, the custom activity (DataDownloader.cs) will download data from a URL (ie. [http://dumps.wikimedia.org/other/pagecounts-raw/](http://dumps.wikimedia.org/other/pagecounts-raw/), uncompress the file, and then upload the data into Microsoft Azure blob storage. 

1. Via Visual Studio, create a .NET class library project and include DataDownloader.cs
2. Install NuGet dependencies:
    - Install-Package Microsoft.Azure.Management.DataFactories
    - Install-Package Azure.Storage
3. Build the DLL and name it according to what is defined in pipeline json (ie. DataDownloaderActivity.dll)
4. Zip the contents of bin/debug or bin/release and upload it to blob storage to the container/path defined in pipeline json (ie. <container>/package/DataDownloaderActivity.zip)

For detailed information about custom activities in general and step-by-step instructions to create a custom activity, see [Use custom activities in an Azure Data Factory pipeline](https://azure.microsoft.com/documentation/articles/data-factory-use-custom-activities/).

### Pipeline

1. `New-AzureDataFactoryPipeline -ResourceGroupName [RESOURCE_GROUP_NAME] -DataFactoryName [DATA_FACTORY_NAME]  -File /Pipelines/DataDownloaderSamplePipeline.json`


## Known Issues

* Manually create the *adfjobs* container in your Azure Storage. When the custom activity runs, Azure Data Factory will be able to capture that output from the HDInsight cluster, and save it in the adfjobs storage container in your Azure Blob Storage account.
