# Deploy-AzureDataFactory
Deploys an Azure Data Factory (ADF) from the command line. 


## Synopsis
The script only deploys linked services, datasets and pipelines for a data factory, no current suport for Gateways.	
For this script to run correctly, in the PowerShell context in which this is called, you should have already loged in
to your Azure account and selected the correct subscription, e.g.:
```
	Login-AzureRmAccount
	Get-AzureRmSubscription –SubscriptionName “YourSubscription” | Select-AzureRmSubscription
```

The script expects the ADF's components to have specific suffixes for each of the elements
(linked service, dataset, pipeline) in order for them to be found and deployed correctly. 
The suffixes are configurable through the *FilenameSuffix set of parameters.

Support for linked services connection strings overwriting is supported as well, 
as long as that is passed in through the DataFactoryConfigFile parameter.

## Code Example
Installs an ADF:
```
 .\Deploy-AzureDataFactory.ps1 -SetupMode INSTALL -ResourceGroupName MyResourceGroup -Name MyDataFactory -InformationAction Continue -Verbose -Location NorthEurope  -DataFactoryDirectory "Z:\MyDataFactoryDirectory\"
```

Upgrades (replaces) an existing ADF and uses a user-specified configuration file:
```
 .\Deploy-AzureDataFactory.ps1 -SetupMode UPGRADE -ResourceGroupName MyResourceGroup -Name MyDataFactory -InformationAction Continue -Verbose -Location NorthEurope  -DataFactoryDirectory "Z:\MyDataFactoryDirectory\" -DataFactoryConfigFile "Z:\MyDataFactoryDirectory\MyConfigFile.json"
```

## Motivation
This was created to be able to fully deploy, update or delete an ADF pipeline from the command line with one single instruction.
