using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Net.Http.Formatting;

using System.Linq;
using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.Azure.Management.DataFactories.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SentimentAnalysisService
{
    public class SentimentAnalysis : IDotNetActivity
    {
        private IActivityLogger _logger;
        private string _storageConnectionString;
        private string _storageContainerName;
        private string _inputBlobName;
        private string _outputBlobName;
        private string _apiKey;
        private string _baseUrl;

        public IDictionary<string, string> Execute(
            IEnumerable<LinkedService> linkedServices, 
            IEnumerable<Dataset> datasets, 
            Activity activity, 
            IActivityLogger logger)
        {
            _logger = logger;

            _logger.Write("######Execute Begin######");

            // to get extended properties (for example: SliceStart)
            DotNetActivity dotNetActivity = (DotNetActivity)activity.TypeProperties;
            string sliceStartTime = dotNetActivity.ExtendedProperties["SliceStart"];
            _logger.Write("Slice start time is : {0}", sliceStartTime);

            _baseUrl = dotNetActivity.ExtendedProperties["baseUrl"];
            if (String.IsNullOrEmpty(_baseUrl))
            {
                _logger.Write("Null or Empty Base URL for ML Model: {0}", _baseUrl);
                throw new Exception(string.Format("Null or Empty Base URL for ML Model: {0}", _baseUrl));
            }
            _logger.Write("Base ML Azure Website url is : {0}", _baseUrl);

            _apiKey = dotNetActivity.ExtendedProperties["apiKey"];
            if (String.IsNullOrEmpty(_apiKey))
            {
                _logger.Write("Null or Empty API Key for ML Model: {0}", _apiKey);
                throw new Exception(string.Format("Null or Empty API Key for ML Model: {0}", _apiKey));
            }

            // declare dataset types
            CustomDataset inputLocation;
            CustomDataset outputLocation;
            AzureStorageLinkedService inputLinkedService;
            AzureStorageLinkedService outputLinkedService;

            // Get the ADF Input Tables
            Dataset inputDataset = datasets.Single(dataset => dataset.Name == activity.Inputs.Single().Name);
            inputLocation = inputDataset.Properties.TypeProperties as CustomDataset;

            inputLinkedService = linkedServices.Single(
                linkedService =>
                linkedService.Name ==
                inputDataset.Properties.LinkedServiceName).Properties.TypeProperties
                as AzureStorageLinkedService;

            _storageConnectionString = inputLinkedService.ConnectionString;

            if (String.IsNullOrEmpty(_storageConnectionString))
            {
                _logger.Write("Null or Empty Connection string for input table: {0}", inputDataset.Name);
                throw new Exception(string.Format("Null or Empty Connection string for input table: {0}", inputDataset.Name));
            }

            string folderPath = GetFolderPath(inputDataset);

            if (String.IsNullOrEmpty(folderPath))
            {
                _logger.Write("Null or Empty folderpath for input table: {0}", inputDataset.Name);
                throw new Exception(string.Format("Null or Empty folder path for input table: {0}", inputDataset.Name));
            }
            _storageContainerName = folderPath.Split('/')[0];
            _inputBlobName = folderPath.Substring(folderPath.IndexOf('/') + 1) ;
            _logger.Write("Folder Path for Input Table {0}: {1}", inputDataset.Name, folderPath);            

            // Get the ADF Output Tables
            Dataset outputDataset = datasets.Single(dataset => dataset.Name == activity.Outputs.Single().Name);
            outputLocation = outputDataset.Properties.TypeProperties as CustomDataset;

            outputLinkedService = linkedServices.Single(
                linkedService =>
                linkedService.Name ==
                outputDataset.Properties.LinkedServiceName).Properties.TypeProperties
                as AzureStorageLinkedService;

            _storageConnectionString = outputLinkedService.ConnectionString;

            folderPath = GetFolderPath(outputDataset);

            if (String.IsNullOrEmpty(_storageConnectionString))
            {
                _logger.Write("Null or Empty Connection string for output table: {0}", outputDataset.Name);
                throw new Exception(string.Format("Null or Empty Connection string for output table: {0}", outputDataset.Name));
            }
            if (String.IsNullOrEmpty(folderPath))
            {
                _logger.Write("Null or Empty folderpath for output table: {0}", outputDataset.Name);
                throw new Exception(string.Format("Null or Empty folder path for output table: {0}", outputDataset.Name));
            }
            _outputBlobName = folderPath.Substring(folderPath.IndexOf('/') + 1) ;
            _logger.Write("Folder Path for Ouput Table {0}: {1}", outputDataset.Name, folderPath);
            
         
            try
            {
                // Invoke ML Batch Execution Service
                InvokeBatchExecutionService().Wait();
            }
            catch (Exception ex)
            {
                _logger.Write("ML Model Call failed with error : {0}", ex.ToString());
                throw;
            }

            return new Dictionary<string, string>();
        }

        async Task InvokeBatchExecutionService()
        {
            // How this works:
            //
            // 1. Tweets present in Azure Blob
            // 2. Call the Batch Execution Service to process the data in the blob. 
            // 3. The results get written to Azure ML blob.
            // 4. Copy the Azure ML output blob to your storage blob.

            var blobClient = CloudStorageAccount.Parse(_storageConnectionString).CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_storageContainerName);
            var blob = container.GetBlockBlobReference(_inputBlobName);

            _logger.Write("Submitting the job...");
            
            // set a time out for polling status
            const int timeOutInMilliseconds = 120 * 100000; // Set a timeout

            using (HttpClient client = new HttpClient())
            {
                BatchScoreRequest request = new BatchScoreRequest()
                {
                    Input = new AzureBlobDataReference()
                    {
                        ConnectionString = _storageConnectionString,
                        RelativeLocation = blob.Uri.LocalPath
                    },
                };
                 
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
           
                var response = await client.PostAsJsonAsync(_baseUrl, request);
                string jobId = await response.Content.ReadAsAsync<string>();

                string jobLocation = _baseUrl + "/" + jobId;
                Stopwatch watch = Stopwatch.StartNew();
                bool done = false;
                while (!done)
                {
                    response = await client.GetAsync(jobLocation);
                    BatchScoreStatus status = await response.Content.ReadAsAsync<BatchScoreStatus>();
                    if (watch.ElapsedMilliseconds > timeOutInMilliseconds)
                    {
                        done = true;
                        _logger.Write("Timed out. Deleting the job ...");
                        await client.DeleteAsync(jobLocation);
                    }
                    switch (status.StatusCode)
                    {
                        case BatchScoreStatusCode.NotStarted:
                            _logger.Write("Not started...");
                            break;
                        case BatchScoreStatusCode.Running:
                            _logger.Write("Running...");
                            break;
                        case BatchScoreStatusCode.Failed:
                            _logger.Write("Failed!");
                            _logger.Write("Error details : {0}", status.Details);
                            throw new Exception(status.Details);
                        case BatchScoreStatusCode.Cancelled:
                            _logger.Write("Cancelled!");
                            throw new Exception(status.Details);
                        case BatchScoreStatusCode.Finished:
                            done = true;
                            _logger.Write("Finished!");
                            var credentials = new StorageCredentials(status.Result.SasBlobToken);
                            var sourceCloudBlob = new CloudBlockBlob(new Uri(new Uri(status.Result.BaseLocation), status.Result.RelativeLocation), credentials);
                            var targetCloudBlob = container.GetBlockBlobReference(_outputBlobName);
                            targetCloudBlob.StartCopy(sourceCloudBlob);
                            _logger.Write("Copy to Output Blob Complete...");
                            break;
                    }

                    if (!done)
                    {
                        Thread.Sleep(1000); // Wait one second
                    }
                }
            }
        }


        /// <summary>
        /// Gets the folderPath value from the input/output dataset.
        /// </summary>
        private static string GetFolderPath(Dataset dataArtifact)
        {
            if (dataArtifact == null || dataArtifact.Properties == null)
            {
                return null;
            }

            AzureBlobDataset blobDataset = dataArtifact.Properties.TypeProperties as AzureBlobDataset;
            if (blobDataset == null)
            {
                return null;
            }

            return blobDataset.FolderPath;
        }
    }

    public class AzureBlobDataReference
    {
        // Storage connection string used for regular blobs. It has the following format:
        // DefaultEndpointsProtocol=https;AccountName=ACCOUNT_NAME;AccountKey=ACCOUNT_KEY
        // It's not used for shared access signature blobs.
        public string ConnectionString { get; set; }

        // Relative uri for the blob, used for regular blobs as well as shared access 
        // signature blobs.
        public string RelativeLocation { get; set; }

        // Base url, only used for shared access signature blobs.
        public string BaseLocation { get; set; }

        // Shared access signature, only used for shared access signature blobs.
        public string SasBlobToken { get; set; }
    }

    public enum BatchScoreStatusCode
    {
        NotStarted,
        Running,
        Failed,
        Cancelled,
        Finished
    }

    public class BatchScoreStatus
    {
        // Status code for the batch scoring job
        public BatchScoreStatusCode StatusCode { get; set; }

        // Location for the batch scoring output
        public AzureBlobDataReference Result { get; set; }

        // Error details, if any
        public string Details { get; set; }
    }

    public class BatchScoreRequest
    {
        public AzureBlobDataReference Input { get; set; }
    }
}
