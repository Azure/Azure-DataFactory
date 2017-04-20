# Introduction
This code contains source code and nuget project for the ADFCustomActivityRunner nuget.
ADF custom activity runner allows you to step into and debug Azure Data Factory (ADF) custom DotNetActivity activities using the information configured in your pipeline.

This package provides an attribute that you can apply to your custom activity method (DotNetActivity) in oder to tell it that the method is a custom activity which can be debugged directly. Inside the attribute you specify the location of the pipeline which your custom activity is run from and the name of the activity in the pipeline. When you wish to debug the activity, this information is used to build the ADF objects (LinkedServices, Datasets, Activity, Logger) used in the custom activity. This allows you to debug the custom activity as if it were running inside Azure Data Factory. You can optionally specify a deployment configuration file if one is used. Also if you are using the ADF Secure Publish Visual Studio extension, it will work with that also so you do not need to expose secrets in your source code. One thing to note however, the first environment which is set in the Secure Publish  user settings will be the one that is picked. This environment will have an associated key vault so if you wish to select a different key vault (which is associated with a different environment), make sure to push that environment to the top of the list.

This package comes with a base class which you inherit instead of implementing IDotNetActivity. This abstract bass class is called CustomActivityBase, it implements IDotNetActivity and has a number of methods that simplify getting information from the ADF Json files, such as:  
GetExtendedProperty(string name)  
GetAllExtendedProperties()  
GetInputSqlConnectionString()  
GetOutputSqlConnectionString()  
GetSqlConnectionString(string datasetName)  
GetLinkedService<T>(string name)  
GetDataset<T>(string name)  
GetBlobFolderPath(string datasetName)  
GetBlob(string datasetName)  
DownLoadFile(string blobFileName)  
DownLoadLatestFile()  
UploadFile(string localFilePath)  
GetLatestBlobFileName()  
RemoveFilesFromBlob()  

# How to use:
1. Create a class library project where you will write your custom activity code and install the ADFCustomActivityRunner nuget package: https://www.nuget.org/packages/ADFCustomActivityRunner
2. Inherit from the abstract base class called CustomActivityBase. 
3. Implement the method 'RunActivity'. This method calls the execute method internally. The arguments for this method are exposed as public properties on the base class:   
    LinkedServices  
    Datasets  
    Activity  
    Logger  
    Add your logic to this method. There are a number of helper methods available within the base class to make it easier to access linked services, datasets, extended properties and perform some basic blob operations such as uploading and downloading.
4. Add the CustomActivity attribute to the RunActivity method and specify the relative location from the custom activity project to the pipeline file and the name of the actviity you wish to target. Optionally the name of the deployment config file to use e.g.:
[CustomActivity(ActivityName = "KickOffSproc", PipelineLocation = @"..\DataFactoryApp\PipelineBlobSample.json", DeployConfig = "Dev.json")] 
5. Run the custom activity as if it were a unit test.

This package uses code from NUnit, DotNetActivityRunner, and ADFSecurePublish packages.
