# HttpDataDownloaderSample

## Introduction

This showcases downloading of data from an HTTP Endpoint to Azure Blob Storage using Azure Data Factory Custom C# Activity. Once the data is downloaded into Azure Blob, it can be consumed for further processing.


To learn more about Custom C# Activity, visit the Documentation Center for 'Azure Data Factory' and read 'Use Custom Activities in a Data Factory Pipeline'

## Contents

* /CustomDotNetActivity - Contains custom activities 
* /LinkedServices - Contains linked services definitions
* /Pipelines - Contains pipeline definition
* /Tables - Contains datasets/table definitions

## Getting Started

### Azure Data Factory

1. `Switch-AzureMode AzureResourceManager `
2. `New-AzureResourceGroup -Name [RESOURCE_GROUP_NAME]  -Location "West US"`
3. `New-AzureDataFactory -ResourceGroupName [RESOURCE_GROUP_NAME] -Name [DATA_FACTORY_NAME] –Location "West US"`

### Linked Services

1. `New-AzureDataFactoryLinkedService -ResourceGroupName [RESOURCE_GROUP_NAME] -DataFactoryName [DATA_FACTORY_NAME] -File /LinkedServices/RawEventsLinkedService.json`

Optionally, if you choose you run your own instance of HDInsight in lieu of the on-demand HDInsight cluster:

1. `New-AzureDataFactoryLinkedService -ResourceGroupName [RESOURCE_GROUP_NAME] -DataFactoryName [DATA_FACTORY_NAME] -File /LinkedServices/HDInsightLinkedService.json`


### Dataset

1. `New-AzureDataFactoryTable -ResourceGroupName [RESOURCE_GROUP_NAME] -DataFactoryName [DATA_FACTORY_NAME] -File /Tables/RawEventsTable.json`

### Custom Activity

Given each time splice, the custom activity (DataDownloader.cs) will download data from a URL (ie. [http://dumps.wikimedia.org/other/pagecounts-raw/](http://dumps.wikimedia.org/other/pagecounts-raw/), uncompress the file, and then upload the data into Microsoft Azure blob storage. 

1. Via Visual Studio, create a .NET class library project and include DataDownloader.cs
2. Install NuGet dependencies:
   * `Install-Package Microsoft.Azure.Management.DataFactories –Pre`
   * `Install-Package Microsoft.DataFactories.Runtime –Pre`
   * `Install-Package Azure.Storage`
3. Build the DLL and name it according to what is defined in pipeline json (ie. DataDownloaderActivity.dll)
4. Zip the contents of bin/debug or bin/release and upload it to blob storage to the container/path defined in pipeline json (ie. <container>/package/DataDownloaderActivity.zip)


### Pipeline

1. `New-AzureDataFactoryPipeline -ResourceGroupName [RESOURCE_GROUP_NAME] -DataFactoryName [DATA_FACTORY_NAME]  -File /Pipelines/DataDownloaderSamplePipeline.json`