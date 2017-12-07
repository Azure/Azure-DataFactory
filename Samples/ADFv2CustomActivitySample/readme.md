# Read Me

This sample demonstrates how you can use Data Factory V2 Custom Activity. It is rewritten based on the DLL and pipeline sample described in Data Factory V1 document [Use custom activities in an Azure Data Factory pipeline](https://docs.microsoft.com/en-us/azure/data-factory/v1/data-factory-use-custom-activities) to show how Data Factory V1 (Custom) DotNet Activity pipeline can be converted to Data Factory V2 Custom Activity style. 

If you are familiar with Data Factory V1 (Custom) DotNet Activity pipeline, some high level differences are: 

- The Datasets are optional for Data Factory V2 Custom Activity. The required properties are passed by activity to the custom executable through ReferenceObjects and ExtendedProperties (see MyCustomActivityPipeline.json). 
- The custom logic is implemented through a custom executable instead of a DLL (see Listall.cs). 
- The executable takes the input parameters by parsing activity.json, linkedServices.json, and datasets.json passed to the same folder (see Listall.cs)
- The executable writes outputs to STDOUT instead of implementing a logger (see Listall.cs)


For detailed difference between Data Factory V2 Custom Activity and Data Factory V1 (Custom) DotNet Activity, refer to following document: 

Difference between Custom Activity in Azure Data Factory V2 and (Custom) DotNet Activity in Azure Data Factory V1
https://docs.microsoft.com/en-us/azure/data-factory/transform-data-using-dotnet-custom-activity#difference-between-custom-activity-in-azure-data-factory-v2-and-custom-dotnet-activity-in-azure-data-factory-v1
