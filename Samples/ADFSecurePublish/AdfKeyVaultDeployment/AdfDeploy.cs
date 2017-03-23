using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hyak.Common;
using Microsoft.Azure;
using Microsoft.Azure.Management.DataFactories;
using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;
using Newtonsoft.Json.Linq;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public class AdfDeploy
    {
        private string resourceManagerEndpoint = "https://management.azure.com/";

        private string resourceGroupName;
        private string dataFactoryName;
        private IAdfFileHelper adfFileHelper;
        private ILogger logger;
        private IBlobUtilities blob;
        private SettingsContext settingsContext;

        public AdfDeploy(IAdfFileHelper adfFileHelper, ILogger logger, IBlobUtilities blob, SettingsContext settingsContext, string resourceGroupName, string dataFactoryName)
        {
            this.logger = logger;
            this.blob = blob;
            this.adfFileHelper = adfFileHelper;
            this.resourceGroupName = resourceGroupName;
            this.dataFactoryName = dataFactoryName;
            this.settingsContext = settingsContext;
        }

        /// <summary>
        /// Deploys the specified ADF files and custom activity packages to Azure.
        /// </summary>
        /// <param name="filesToProcess">The files to process.</param>
        /// <param name="outputFolder">The output folder which contains the files and custom activity zips.</param>
        /// <param name="deployConfig">The deployment configuration information.</param>
        public async Task<bool> Deploy(List<string> filesToProcess, string outputFolder, DeployConfigInfo deployConfig = null)
        {
            bool result = true;
            logger.Write(string.Empty);
            logger.Write($"Getting all ADF resources to deploy to Azure from output folder '{outputFolder}'", "Black");

            List<Task<AdfFileInfo>> allFilesTasks = filesToProcess.Select(async x => await adfFileHelper.GetFileInfo(x, deployConfig)).ToList();
            List<AdfFileInfo> allFiles = new List<AdfFileInfo>();

            foreach (var allFilesTask in allFilesTasks)
            {
                allFiles.Add(await allFilesTask);
            }

            List<AdfFileInfo> validFiles = allFiles.Where(x => x.IsValid).ToList();

            if (!validFiles.Any())
            {
                logger.Write($"No valid ADF files found in '{outputFolder}'", "Red");
                return false;
            }

            logger.Write($"{validFiles.Count} file{(validFiles.Count == 1 ? string.Empty : "s")} retreived");
            logger.Write(string.Empty);
            logger.Write($"Begin deploying ADF resources to '{dataFactoryName}'", "Black");

            // Log invalid files
            List<AdfFileInfo> invalidFiles = allFiles.Where(x => !x.IsValid).ToList();

            if (invalidFiles.Any())
            {
                logger.Write("The following files found in the output folder will not be published:");

                foreach (AdfFileInfo invalidFile in invalidFiles)
                {
                    logger.Write(invalidFile.FileName);
                }
            }

            DataFactoryManagementClient client = GetDataFactoryManagementClient();

            // Deploy Package Zips before deploying ADF JSON files
            List<AdfFileInfo> pipelines = validFiles.Where(x => x.FileType == FileType.Pipeline).ToList();
            List<CustomActivityPackageInfo> packages = pipelines.SelectMany(x => x.CustomActivityPackages).Distinct().ToList();

            if (packages.Any())
            {
                result &= await DeployCustomActivities(packages, validFiles, outputFolder);
            }

            List<AdfFileInfo> linkedServices = validFiles.Where(x => x.FileType == FileType.LinkedService).ToList();

            if (linkedServices.Any())
            {
                logger.Write(string.Empty);
                logger.Write("Deploying LinkedServices", "Black");

                // Deploy non batch linked services first
                var linkedServiceTaskDict = new Dictionary<string, Task<LinkedServiceCreateOrUpdateResponse>>();

                foreach (var linkedService in linkedServices.Where(x => !x.SubType.Equals("AzureBatch", StringComparison.InvariantCultureIgnoreCase)))
                {
                    linkedServiceTaskDict.Add(linkedService.Name,
                        client.LinkedServices.CreateOrUpdateWithRawJsonContentAsync(resourceGroupName,
                            dataFactoryName, linkedService.Name,
                            new LinkedServiceCreateOrUpdateWithRawJsonContentParameters(linkedService.FileContents)));
                }

                foreach (var item in linkedServiceTaskDict)
                {
                    try
                    {
                        LinkedServiceCreateOrUpdateResponse response = await item.Value;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            logger.Write($"Linked service '{response.LinkedService.Name}' uploaded successfully.", "Green");
                        }
                        else
                        {
                            logger.Write($"Linked service '{response.LinkedService.Name}' did not upload successfully. Response status: {response.Status}", "Red");
                            result = false;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Write($"Linked service '{item.Key}' did not upload successfully. Error: {e.Message}", "Red");
                        logger.WriteError(e);
                        result = false;
                    }
                }

                // Deploy batch linked services next
                var batchLinkedServiceTaskDict = new Dictionary<string, Task<LinkedServiceCreateOrUpdateResponse>>();

                foreach (var batchLinkedService in linkedServices.Where(x => x.SubType.Equals("AzureBatch", StringComparison.InvariantCultureIgnoreCase)))
                {
                    batchLinkedServiceTaskDict.Add(batchLinkedService.Name,
                        client.LinkedServices.CreateOrUpdateWithRawJsonContentAsync(resourceGroupName,
                            dataFactoryName, batchLinkedService.Name,
                            new LinkedServiceCreateOrUpdateWithRawJsonContentParameters(batchLinkedService.FileContents)));
                }

                foreach (var item in batchLinkedServiceTaskDict)
                {
                    try
                    {
                        LinkedServiceCreateOrUpdateResponse response = await item.Value;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            logger.Write($"Linked service '{response.LinkedService.Name}' uploaded successfully.", "Green");
                        }
                        else
                        {
                            logger.Write($"Linked service '{response.LinkedService.Name}' did not upload successfully. Response status: {response.Status}", "Red");
                            result = false;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Write($"Linked service '{item.Key}' did not upload successfully. Error: {e.Message}", "Red");
                        logger.WriteError(e);
                        result = false;
                    }
                }
            }

            List<AdfFileInfo> tables = validFiles.Where(x => x.FileType == FileType.Table).ToList();

            if (tables.Any())
            {
                logger.Write(string.Empty);
                logger.Write("Deploying tables", "Black");

                // Deploy tables next
                var tableTaskDict = new Dictionary<string, Task<DatasetCreateOrUpdateResponse>>();
                foreach (AdfFileInfo adfJsonFile in tables)
                {
                    try
                    {
                        Task<DatasetCreateOrUpdateResponse> tableTask = client.Datasets.CreateOrUpdateWithRawJsonContentAsync(resourceGroupName, dataFactoryName,
                           adfJsonFile.Name, new DatasetCreateOrUpdateWithRawJsonContentParameters(adfJsonFile.FileContents));

                        tableTaskDict.Add(adfJsonFile.Name, tableTask);
                    }
                    catch (Exception e)
                    {
                        logger.Write($"An error occurred uploading table '{adfJsonFile.Name}': " + e.Message, "Red");
                        logger.WriteError(e);
                        result = false;
                    }
                }

                foreach (var task in tableTaskDict)
                {
                    try
                    {
                        DatasetCreateOrUpdateResponse response = await task.Value;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            logger.Write($"Table '{task.Key}' uploaded successfully.", "Green");
                        }
                        else
                        {
                            logger.Write(
                                $"Table '{task.Key}' did not upload successfully. Response status: {response.Status}",
                                "Red");
                            result = false;
                        }
                    }
                    catch (CloudException ex)
                    {
                        if (ex.Error.Code == "TableAvailabilityUpdateNotSupported")
                        {
                            logger.Write($"It looks like you are trying to change the availability for the Table '{task.Key}'. Currently this is not supported by ADF so as work around you should delete the dataset and related pipleline in the Data Factory '{dataFactoryName}' and run the publish again.", "Red");
                        }
                        else
                        {
                            logger.Write($"Table '{task.Key}' did not upload successfully. An error occurred: " + ex.Message, "Red");
                        }

                        logger.WriteError(ex);
                        result = false;
                    }
                    catch (Exception ex)
                    {
                        logger.WriteError(ex);
                        logger.Write($"Table '{task.Key}' did not upload successfully. An error occurred: " + ex.Message, "Red");
                        result = false;
                    }
                }
            }

            if (pipelines.Any())
            {
                logger.Write(string.Empty);
                logger.Write("Deploying pipelines", "Black");

                // Deploy pipelines last
                var pipelineTaskDict = new Dictionary<string, Task<PipelineCreateOrUpdateResponse>>();
                foreach (AdfFileInfo adfJsonFile in pipelines)
                {
                    pipelineTaskDict.Add(adfJsonFile.Name, client.Pipelines.CreateOrUpdateWithRawJsonContentAsync(resourceGroupName, dataFactoryName,
                        adfJsonFile.Name, new PipelineCreateOrUpdateWithRawJsonContentParameters(adfJsonFile.FileContents)));
                }

                foreach (var item in pipelineTaskDict)
                {
                    try
                    {
                        PipelineCreateOrUpdateResponse response = await item.Value;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            logger.Write($"Pipeline '{response.Pipeline.Name}' uploaded successfully.", "Green");
                        }
                        else
                        {
                            logger.Write($"Pipeline '{response.Pipeline.Name}' did not upload successfully. Response status: {response.Status}", "Red");
                            result = false;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.WriteError(e);
                        logger.Write($"An error occurred uploading pipeline '{item.Key}': " + e.Message, "Red");
                        result = false;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Deploys the custom activities.
        /// </summary>
        /// <param name="packages">The packages.</param>
        /// <param name="allFiles">All files.</param>
        /// <param name="outputFolder">The output folder.</param>
        private async Task<bool> DeployCustomActivities(List<CustomActivityPackageInfo> packages, List<AdfFileInfo> allFiles, string outputFolder)
        {
            logger.Write(string.Empty);
            logger.Write("Deploying custom activity packages to blob storage", "Black");

            var tasks = new List<Task<bool>>();

            foreach (CustomActivityPackageInfo package in packages)
            {
                // Get connection string for blob account to upload to
                JObject jObject = allFiles.FirstOrDefault(x => x.Name == package.PackageLinkedService)?.JObject;

                var parts = package.PackageFile.Split('/');

                if (parts.Length != 2)
                {
                    throw new Exception("packageFile should have only one '/' in it. Current packageFile value: " + package.PackageFile);
                }

                string connectionString = jObject?.SelectToken("$.properties.typeProperties.connectionString").ToObject<string>();
                string localFilePath = Path.Combine(outputFolder, parts[1]);
                string blobFolder = parts[0];

                tasks.Add(blob.UploadFile(localFilePath, connectionString, blobFolder));
            }

            var packageUploadResults = await Task.WhenAll(tasks);

            return packageUploadResults.All(x => x);
        }

        /// <summary>
        /// Gets the data factory management client.
        /// </summary>
        private DataFactoryManagementClient GetDataFactoryManagementClient()
        {
            TokenCloudCredentials aadTokenCredentials =
                new TokenCloudCredentials(settingsContext.SubscriptionId,
                    AzureAccessUtilities.GetAuthorizationHeaderNoPopup(settingsContext));

            Uri resourceManagerUri = new Uri(resourceManagerEndpoint);

            DataFactoryManagementClient client = new DataFactoryManagementClient(aadTokenCredentials, resourceManagerUri);

            return client;
        }
    }
}
