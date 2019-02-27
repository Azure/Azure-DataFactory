# RunRScriptUsingADFSample

## Introduction

This sample includes the ADF Custom Activity class that can be used to invoke RScript.exe. 
The sample only contains the code for the C# class file and doesn't include any ADF linked service, pipeline and dataset jsons.
This sample will only work with BYOC HDInsight cluster that already has R Installed on it.

In this sample, we do the following:

a. Invoke the Custom .Net Activity with parameters that a sample R Script might expect.
b. Download input files from Azure Storage to be used during R execution.
c. Performs the R Execution.
d. Upload output files after R execution to Azure Storage.
