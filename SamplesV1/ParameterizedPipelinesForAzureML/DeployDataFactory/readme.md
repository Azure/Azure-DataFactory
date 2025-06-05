# Creating Multiple Parameterized Pipelines with AzureML Activities using Data Factory SDK #

Data factory batch execution scoring and retraining activities support calling into corresponding scoring and retraining web services generated from experiments created in azure machine learning studio.

Readers are encouraged to first review the [overview of creating pipelines with machine learning activities](https://azure.microsoft.com/en-us/documentation/articles/data-factory-azure-ml-batch-execution-activity/) to understand how data factory helps you integrate with machine learning. 

Often times customers start by creating a machine learning model and corresponding scoring and retraining pipelines for some real world entity like an energy plant or an oil well for an example. A lot of the times this pipeline is developed using data factory's web editor for authoring pipelines.

However once the model and pipeline are tested and ready to be deployed to PROD at scale, for many scenarios customers find that they need a way to pass parameters end to end to represent one of N real world entities. For example the energy plants could be distributed across different regions over the world and you need one pipeline per region.

The best way to take an existing pipeline and creating N instances of it with different parameters is to use data factory's C# SDK.

This sample provides  end to end C# code to deploy N pipelines for scoring and retraining each with a different **region** parameter where the list of regions is coming from a parameters.txt file which is included with this sample. 

Before moving on please refer to the [overview for using data factory C# SDK to create & manage pipelines.](https://azure.microsoft.com/en-us/documentation/articles/data-factory-create-data-factories-programmatically/)

## Overview of end to end flow of parameters ##

Here are the high level steps for configuring your experiment, web services and data factory pipelines.

1. Configure your scoring and retraining experiment in Azure ML studio to receive region as a parameter value. 
2. This sample assumes the ML experiment is using SQL reader module to read its data and calls a stored procedure to fetch the data. The experiment takes the region value as one of the global parameters passed for the default web service input and passes it onto the stored procedure as a parameter.
3. Create scoring and retraining web services.
4. Create N scoring endpoints pro-grammatically. Each endpoint will host the model for a specific region.    
5. Create N pipelines for scoring & N pipelines for retraining. The pipeline will calls the appropriate endpoint for the given region. The sample here and steps below provide more information on how to configure this.

## Configuring the sample ##

There are a few things you would need to do to configure this sample.

1. Add you subscription (SubscriptionId) & active Directory tenant ID (ActiveDirectoryTenantId) information in App.config file.
2. Add a new GUID to represent your data factory client app (this sample running on behalf of your organization) - AdfClientId value.
3. There are various values in DataFactoryConfig file that you need to configure like data factory name, region for deployment, storage account information etc. Please configure all the values before proceeding.

Once you have configured the values you can run the program. The program will first ask for your Azure credentials and once logged in will deploy pipelines in the data factory with the given name.


