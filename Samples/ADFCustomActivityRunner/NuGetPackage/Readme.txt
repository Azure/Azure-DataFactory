ADF custom activity runner allows you to step into and debug Azure Data Factory (ADF) custom DotNetActivity activities using the information configured in your pipeline.

This package provides an attribute that you can apply to your DotNetActivity. You specify the location of the pipeline which your custom activity is run from and the name of the activity in the pipeline. The activity is then de-serialized into an object which you can use to easily debug your custom activity in the context of the pipeline it is found in. You can optionally specify a deployment configuration file if one is used.

This package comes with a base class which you inherit instead of implementing IDotNetActivity. This abstract bass class is called CustomActivityBase, it implements IDotNetActivity and has a number of methods that simplify getting information from the ADF Json files. 

How to use: 
1. Inherit from the abstract base class called CustomActivityBase. 
2. Implement the method RunActivity. This method calls the execute method internally. The arguments for this method are exposed as public properties on the base class: 
    LinkedServices
    Datasets
    Activity
    Logger
3. Add the CustomActivity attribute to the RunActivity method and specify the relative location from the custom activity project to the pipeline file and the name of the actviity you wish to target. Optionally the name of the deployment config file to use e.g.:
[CustomActivity(ActivityName = "KickOffSproc", PipelineLocation = @"..\DataFactoryApp\PipelineBlobSample.json", DeployConfig = "Dev.json")] 
4. Run the custom activity as if it were a unit test.

This also works with ADF Secure publish so you do not need to expose connection strings in the ADF linked service files. Instead they will be read in from Azure Key Vault. See https://github.com/Azure/Azure-DataFactory/tree/master/Samples/ADFSecurePublish for more information on this.

This package uses code from NUnit and DotNetActivityRunner packages.