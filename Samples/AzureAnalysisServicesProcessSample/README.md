# Introduction
A very common action at the end of an ETL process is to reprocess a tabular model. To do so in Data Factory a Custom Activity is needed.
This example implements a Custom Activity capable of reprocess a model or execute a custom processing script (for example, merge partitions) in Azure Analysis Services. 

If user specifies AdvancedASProcessingScriptPath in extendedProperties of DotNetActivity, the sample will process the database based on the TMSL script definition.
If AdvancedASProcessingScriptPath is not specified, the sample by default does a full process of specified database. 


# Considerations
This sample uses Analysis Services AMO and ADOMD version 14.0.1.209. It is suggested that you always check below link for latest client libraries: 

[Client libraries for connecting to Azure Analysis Services](https://docs.microsoft.com/en-us/azure/analysis-services/analysis-services-data-providers)

The Custom Activity of ADF runs against Azure Batch. If you would like to install the latest MSOLAP native OLEDB provider, consider deploying it as an application package per below document: 

[Deploy applications to compute nodes with Batch application packages](https://docs.microsoft.com/en-us/azure/batch/batch-application-packages)

# How to use
An example of use can be found on the folder datafactory, that contains Json files composing a very simple pipeline.
Most parameters are common to all Custom Activities, but there are extended properties that are specific of this one:

- AzureASConnectionString: Connection String of the Azure AS instance.
- TabularDatabaseName: Name of the database in the tabular model to be processed.
- AdvancedASProcessingScriptPath: [Optional] The path to TMSL script for custom process. 
  Multiple TMSL scripts can be specified, concatenated by semicolon, e.g. "container1/script1.tmsl;container1/folder/script2.tmsl"

# Service principal authentication
Instead of an Azure AD User, you may wish to use a service principal (also known as Application Registration).
In Azure AS, use this special format to assign a service principal to a role: "app:<CLIENT_ID>@<TENANT_ID>".

Use the following extended properties of the Custom Activity to allow it to retrieve an access token from Azure AD:

- AzureADAuthority: Azure AD authority.
- AzureADResource: Azure AD resource (token scope).
- AzureADClientId: Your service account Client ID. Also known as Application ID.
- AzureADClientSecretPath: Path to a text file in the linked Azure Storage account, containing your service account Client Secret (also known as API Access Key).

