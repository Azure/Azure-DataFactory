# Untar Azure File With Azure Function Sample

## Introduction

This samples illustrates an Azure Data Factory pipeline that will iterate through tar files in an Azure File Share, and extract their content. The basic flow is:

1. Get the metadata from a dataset associated with the Azure File Share
1. Loop through the children of the dataset metadata
1. Pass each file name to an Azure Function
1. The Function downloads the file
1. The contents of the file is extracted to local storage using Adam Hathcock's [SharpCompress library](https://github.com/adamhathcock/sharpcompress)
1. The file is uploaded to the file share
1. The Function returns a list of the urls of the files that have been created, to the Data Factory

## Running and debugging

### Prerequisites

You will need Git, an Azure Subscription, Powershell and the [Az Powershell Module](https://docs.microsoft.com/en-us/powershell/azure/overview?view=azps-1.2.0) to run the sample. You can run it locally or from the [Azure Cloud Shell](https://docs.microsoft.com/en-us/azure/cloud-shell/quickstart-powershell). For local debugging, you will also need Visual Studio 2017, or Visual Studio Code, with the Azure Functions extensions installed.

### Deployment

1. Clone this git repository to your machine with git.
1. Navigate to the folder `{repository location}\Azure-DataFactory\Samples\UntarAzureFilesWithAzureFunction\env`
1. Login to Azure by running `Connect-AzAccount`
1. Select your desired subscription using `Select-AzContext '{subscription name}'`
1. Run the following command in powershell: `deploy.ps1`. You can specify the resource group to use using the -resourcegroupname parameter. A random prefix is automatically generated for all resources to ensure unique names. To prevent new resources from being  created on each run, you can override this prefix with a fixed value by specifying the uniqueresourcenameprefix parameter. Example: `deploy.ps1 -resourcegroupname 'dffunctionssample' -uniqueresourcenameprefix 'e1064086576241d39'`

The deployment script will do the following:

1. Create all the resources you need in the specified resource group. These include a function app, storage account, and data factory.
1. Deployed a pre-existing function app package to the created Function app. You can override this by deploying the app in the `\src` directory using the  Azure Functions Core Tools or Visual Studio, to the Function app that the script generates.
1. Upload the tar files in the env directory to the newly created storage account.
1. Create a ample pipeline and connected services in the created Data Factory.

### Running the sample

1. Open the Azure portal and navigate to the newly created Resource Group.
1. The resource group will contain the Azure Function App, a Storage Account and a Data Factory.
1. Open the Storage account in a new window.
1. Click on _Files_.
1. Click on the _filedrop_ share.
1. You should see two tar files in here. You can click the _connect_ button for instructions to mount the file share on your local machine.
1. In your previous window, open the Data Factory, and click _Author and Monitor_.
1. Click the _Author_ button in the left menu, and select the _UntarPipeline_ pipeline.
1. Click the _Debug_ button.
1. Once the run is completed, the run output should contain an entry for each step.
1. Click on the _output_ button of each Azure Function step. It will display the URL of the extracted files for each tar.
1. Switch back to the window displaying the Azure Files content, and hit the _refresh_ button. The list will now contain two directories containing the content of the tar files.
1. Open the directory to view the contents of the tar files.

### Debugging the Azure Function locally

You can debug the Azure Function on your local machine, by setting up a tunnel using [ngrok](http://ngrok.com/).

First you need to be able to run the Function App in the src folder locally. You will need Either Visual Studio 2017 with the Function Apps extension, or Visual Studio Code with the Azure Functions extension installed. Follow the instructions [here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local).

Once the app is running, make a note of the uri on which it is exposed. This is usually `http://localhost:7071/api/DecompressFile` You can test it by sending a POST request using [Postman](https://www.getpostman.com/) to this URI. You need to set the filename in the body of the post, using the following request body:

```json
{
    "fileName": "TestData1.tar"
}
```

Executing the request will give you a response containing the urls of the files it created. If you are debugging the Function App, it will stop at any breakpoints you've set.

To test your local function using the data factory, you need to set up a tunnel using ngrok:

1. Download ngrok to your local machine.
1. Open a command prompt and run the following command from the location of the ngrok executable. `ngrok http 7071` where 7071 matches the port in the URI you are currently debugging.
1. Copy the https ngrok URI created by ngrok. It will look like `https://e7c0d779.ngrok.io`
1. Replace the address in Postman with the ngrok address, and repeat your test.
1. Now open the Azure Datafactory, and lick the _Connections_ button.
1. Click the _AzureFunctionService_ _Edit_ button.
1. Replace the _Function App URL_ with the address that ngrok created.
1. Type anything in the _Function Key_ field. This function is set to allow anonymous access, so the key doesn't matter.
1. Click Finish.
1. Open the pipeline again, and click _Debug_. After a few moments the breakpoint in your debugger will be hit.

## Notes

Rerunning the factory will result in some failures when the directories created by the function is sent to the Function app. You can prevent this by either deleting the directories between runs, or updating the filepath filter in the Data Factory's FileShareDataset to only look for tar files.

The Azure Function will work with any compression and archive format supported by the [SharpCompress library](https://github.com/adamhathcock/sharpcompress)

More info on the Azure Functions activity is available [here](https://docs.microsoft.com/en-us/azure/data-factory/control-flow-azure-function-activity)