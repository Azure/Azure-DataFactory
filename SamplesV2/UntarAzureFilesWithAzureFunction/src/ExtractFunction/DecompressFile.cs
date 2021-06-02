using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Newtonsoft.Json;

namespace ExtractFunction
{
    public static class DecompressFile
    {
        [FunctionName("DecompressFile")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            dynamic data = JsonConvert.DeserializeObject(await new StreamReader(req.Body).ReadToEndAsync());
            string inputFileName = data.fileName;
            log.LogInformation("Request received to extract file {inputFileName}.", inputFileName);
            CloudFileClient fileClient = LoadFileClient();
            CloudFileShare sourceFileShare =
                fileClient.GetShareReference(Environment.GetEnvironmentVariable("SourceFileShareName"));
            CloudFileDirectory rootDirectory = sourceFileShare.GetRootDirectoryReference();

            CloudFile sourceFile = rootDirectory.GetFileReference(inputFileName);

            if (!await sourceFile.ExistsAsync())
            {
                return new NotFoundObjectResult("Source file does not exist.");
            }

            CloudFileDirectory destinationDirectory = await LoadDestinationDirectory(sourceFile, rootDirectory);

            dynamic returnObject = await ExtractionManager.ExtractAndUploadFiles(log, sourceFile, destinationDirectory);

            return new OkObjectResult(returnObject);
        }

        private static async Task<CloudFileDirectory> LoadDestinationDirectory(CloudFile sourceFile, CloudFileDirectory rootDirectory)
        {
            string outputDirectoryName = $"{sourceFile.Name}.content";
            
            CloudFileDirectory destinationDirectory = rootDirectory.GetDirectoryReference(outputDirectoryName);
            await destinationDirectory.CreateIfNotExistsAsync();
            return destinationDirectory;
        }

        private static CloudFileClient LoadFileClient()
        {
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("FileStorageConnectionString"));
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
            return fileClient;
        }
    }
}
