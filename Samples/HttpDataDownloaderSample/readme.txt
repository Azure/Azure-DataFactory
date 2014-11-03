Downloading Data from HTTP Endpoint to Azure Blob Using ADF Custom C# Activity
Using Custom C# Activity in Azure Data Factory, you can download data from an HTTP Endpoint to an Azure Blob location. Once the data is downloaded into Azure Blob, it can be consumed for further processing.

To learn more about Custom C# Activity, visit the Documentation Center for 'Azure Data Factory' and read 'Use Custom Activities in a Data Factory Pipeline'

DataDownloaderCustomC#Sample:
This sample contains the following:
1.CustomDataDownLoader C# Class file
2. Azure Data Factory Linked Services, Tables, Pipeline Jsons

This project does the following:

Downloads the data from an Http End Point and loads it into the blob.
You will use the class file provided to construct a C# dll project and zip the contents of the bin/debug directory and upload that to the storage account.
You will refer the path of the uploaded zip in your pipeline Json. For more information, visit the Documentation Center and read 'Use Custom Activities in a Data Factory Pipeline'

For example: 
The user can specify 
"http://dumps.wikimedia.org/other/pagecounts-raw/{0}/{0}-{1}/pagecounts-{0}{1}{2}-{3}00{4}.gz" as an HTTP URL. 
The Custom C# Activity will download the data after hitting the URL and load the data into a Windows Azure blob. 
The downloaded data corresponds to the raw data which contains a single row for each hit made to any Wiki Site 
(For example: Wikimedia, Wikipedia etc.). The data will be download from the Wiki endpoint in compressed form. 
The sample will decompress the data before loading it into the blob. 
