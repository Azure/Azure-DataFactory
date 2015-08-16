## Setting up Azure data factory pipeline ##

The steps below assume basic familiarity with key data factory concepts like linked services, datasets, pipeline, activities etc. The steps also assume you know how to create and work with these JSON objects in the data factory editor on Azure portal.

If you are not familiar with data factory and data factory editor here are a couple of articles you can use to get up to speed first.

1. [Introduction to data factory.](https://azure.microsoft.com/en-us/documentation/articles/data-factory-introduction/)
2. [Build your first pipeline tutorial](https://azure.microsoft.com/en-us/documentation/articles/data-factory-build-your-first-pipeline/) (follow the Using data factory editor tab after you have read through the overview).

The JSON objects referred in the steps below can also be found in the repo under [Data Factory JSONs](https://github.com/hirenshahms/Azure-DataFactory/tree/master/Samples/ReferenceDataRefreshForASAJobs/Data%20Factory%20JSONs) folder.

Here is a quick summary of the setup. The steps below show how to setup a pipeline with a copy activity that runs every 15 minutes and copies the customertable from an Azure SQL database to Azure blob storage. The data for each 15 minute interval is stored in a unique path containing the date & time values for that specific interval as shown in the solution architecture diagram above.

###1. Create the data factory.###

Please refer to steps in the first pipeline tutorial on how to do this.

###2. Create linked service for Azure SQL###

Using data factory editor create the linked service for Azure SQL that provides data factory with the credential and catalog information to connect to the right Azure SQL database that contains your customer table. Fill in the required fields in the template below.

    {
    "name": "AzureSqlLinkedService",
    "properties": {
        "type": "AzureSqlDatabase",
        "typeProperties": {
            "connectionString": "Data Source=<data source>;Initial Catalog=<catalog name>;User ID=<user id>;Password=<password>"
        }
    }
	}

###3. Create linked service for Azure storage###

Using data factory editor create the linked service for your azure storage account. This provides data factory the account information to connect to the right blob storage account. This is the same blob storage account that the ASA job is using to read the reference data from. Fill in the required fields in the template below.

	{
    "name": "StorageLinkedService",
    "properties": {
        "type": "AzureStorage",
        "typeProperties": {
            "connectionString": "DefaultEndpointsProtocol=https;AccountName=<account name>;AccountKey=<key>"
        }
    }
	}

###4. Create the input dataset for Azure SQL table

Use the editor to create the input dataset for the Azure SQL table. Note the following:

a. The table name in the Azure SQL database in the sample below is "customerinfo". Change this if you want your table name to be different.

b. The input dataset references the linked service for Azure SQL we created earlier for connection information.



	{
    "name": "CustomerTableSQL",
    "properties": {
        "published": false,
        "type": "AzureSqlTable",
        "linkedServiceName": "AzureSqlLinkedService",
        "typeProperties": {
            "tableName": "customerinfo"
        },
        "availability": {
            "frequency": "Minute",
            "interval": 15,
            "style": "StartOfInterval"
        },
        "external": true,
        "policy": {}
    }
	}

###5. Create the output dataset for Azure blob

Use the editor to create the output dataset for Azure blob. Note the following:

1. The filePath and folderPath are setup as follows. Be sure to create a container called "satest" in your blob storage account. The path is parameterized to include the specific 15 minute interval.

	"fileName": "customertable.csv",

	"folderPath": "satest/referencedata/{Year}/{Month}/{Day}/{Hour}/{Minute}",


		{
		"name": "CustomerTableBlob",
		"properties": {
		    "published": false,
		    "type": "AzureBlob",
		    "linkedServiceName": "StorageLinkedService",
		    "typeProperties": {
		        "fileName": "customertable.csv",
		        "folderPath": "satest/referencedata/{Year}/{Month}/{Day}/{Hour}/{Minute}",
		        "format": {
		            "type": "TextFormat",
		            "columnDelimiter": ","
		        },
		        "partitionedBy": [
		            {
		                "name": "Year",
		                "value": {
		                    "type": "DateTime",
		                    "date": "SliceEnd",
		                    "format": "yyyy"
		                }
		            },
		            {
		                "name": "Month",
		                "value": {
		                    "type": "DateTime",
		                    "date": "SliceEnd",
		                    "format": "MM"
		                }
		            },
		            {
		                "name": "Day",
		                "value": {
		                    "type": "DateTime",
		                    "date": "SliceEnd",
		                    "format": "dd"
		                }
		            },
		            {
		                "name": "Hour",
		                "value": {
		                    "type": "DateTime",
		                    "date": "SliceEnd",
		                    "format": "HH"
		                }
		            },
		            {
		                "name": "Minute",
		                "value": {
		                    "type": "DateTime",
		                    "date": "SliceEnd",
		                    "format": "mm"
		                }
		            }
		        ]
		    },
		    "availability": {
		        "frequency": "Minute",
		        "interval": 15,
		        "style": "StartOfInterval"
		    }
		}
		}

###5. Create the pipeline###

We bring it all together by creating a pipeline with a copy activity that takes the Azure SQL dataset we created as input and produces the blob dataset we setup above as output every 15 minutes.

Be sure to adjust the following JSON for the following before entering in data factory editor.

1. Give valid start and end datetime. This time is in ISO format with UTC time. Typically you want to give a start date as today's date and end date at least 1-2 days from today. Note: data factory is designed to automatically backfill the copy tasks for intervals in the past. So depending on the time of day you do this you will see copy tasks getting executed for all the hours since mid night for the given day right away. Once testing is done end date can be set to a future date like 9999-09-09 to make sure the pipeline executes continuously.

2. The "blobWriteAddHeader" : true setting tells data factory to add the schema information in the output csv file as the first row. The ASA job uses the schema information to identify the column names in the ASA query shown in the ASA job section.

		{
		"name": "SATestReferenceDataCopy",
		"properties": {
		    "description": "Copy reference data from a Azure SQL table to blob",
		    "activities": [
		        {
		            "type": "Copy",
		            "typeProperties": {
		                "source": {
		                    "type": "SqlSource"
		                },
		                "sink": {
		                    "type": "BlobSink",
		                    "blobWriterAddHeader": true,
		                    "writeBatchSize": 0,
		                    "writeBatchTimeout": "00:00:00"
		                }
		            },
		            "inputs": [
		                {
		                    "name": "CustomerTableSQL"
		                }
		            ],
		            "outputs": [
		                {
		                    "name": "CustomerTableBlob"
		                }
		            ],
		            "policy": {
		                "timeout": "01:00:00",
		                "concurrency": 1,
		                "executionPriorityOrder": "NewestFirst",
		                "style": "StartOfInterval"
		            },
		            "scheduler": {
		                "frequency": "Minute",
		                "interval": 15,
		                "style": "StartOfInterval"
		            },
		            "name": "CopyFromSQLToBlob",
		            "description": "Copy reference customer data from a Azure SQL table to blob"
		        }
		    ],
		    "start": "2015-07-23T17:00:00Z",
		    "end": "2015-07-27T00:00:00Z",
		    "isPaused": false
		}
		}

As soon as you deploy the pipeline you should see data factory create the corresponding copy tasks and begin copying reference data at every 15 minute interval and the ASA job picking up the latest copy of the reference data. Note: Once the data is copied to blob there is an additional 15 minute delay before which ASA job picks up the reference data. Hence the total delay for a change in the reference data table to take effect with the ASA job is 30 minutes.

