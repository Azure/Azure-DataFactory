# Introduction
A very common action at the end of an ETL process is to reprocess a tabular model. To do so in Data Factory a Custom Activity is needed.
This example implements a Custom Activity capable of reprocess a model in Azure Analysis Services. 

This sample will reprocess the entire model, but it would be easy to improve it to allow partial processing, specifing which tables or even which partitions to process.


# Considerations
The Microsoft.AnalysisServices.Tabular nuget package currently available fails when trying to connect to Azure Analysis Services.
Version 13.0.4001.0 of the dlls is needed. This version is part of the SQL 2016 SP1 but we include the dlls here to make things easier.

# How to use
An example of use can be found on the folder datafactory, that contains three Json files composing a very simple pipeline.
Most parameters are common to all Custom Activities, but there are two extended properties that are specific of this one:

- AzureASConnectionString: Connection String of the Azure AS instance.
- TabularDatabaseName: Name of the database in the tabular model to be processed.


