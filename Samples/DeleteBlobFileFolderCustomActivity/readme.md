# DeleteBlobFileFolderCustomActivity

## Introduction

Customers have a requirement wherein they want to delete the files from the source Azure Blob location once the files have been copied. This sample showcases a C# file which can be used as part of ADF custom .net activity to delete particular blobs or an entire folder. You can also find a sample pipeline json on how to invoke the ADF Custom .Net activity.

## Contents

* DeleteFromBlobActivity.cs - C# file to be used as part of ADF Custom .Net activity to delete blob folders 
* PipelineSample.json - Showcases how to invoke the ADF Custom .Net delete blob activity. Replace placeholders corresponding to datasets names, schedule and linked services in the sample pipeline json.

### Custom Activity

Provide a list of Azure Blob datasets to be deleted as a comma sepearted list in the 'inputToDelete' extended property in your pipeline json. The custom .Net activity will retrieve the dataset folderpath and filename property. In case folderpath is only specified, it will delete all the contents of the blob folder.

Note: ADF custom .Net activity cannot reference higher version of storage lib directly. So, please refer to the workaround here: https://github.com/Azure/Azure-DataFactory/tree/master/Samples/CrossAppDomainDotNetActivitySample