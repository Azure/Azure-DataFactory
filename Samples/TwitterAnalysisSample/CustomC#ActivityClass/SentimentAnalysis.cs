using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.DataFactories.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SentimentAnalysisService
{
    public class SentimentAnalysis : ICustomActivity
    {
        private IActivityLogger _logger;
        private string _storageConnectionString;
        private string _storageContainerName;
        private string _inputBlobName;
        private string _outputBlobName;
        private string _apiKey;
        private string _baseUrl;

        public IDictionary<string, string> Execute(
           IEnumerable<ResolvedTable> inputTables,
           IEnumerable<ResolvedTable> outputTables,
           IDictionary<string, string> inputs,
           IActivityLogger activityLogger)
        {
            _logger = activityLogger;
            string sliceStartTime = inputs["sliceStart"];
            _logger.Write(TraceEventType.Information, "Slice start time is : {0}", sliceStartTime);

            _baseUrl = inputs["baseUrl"];
            if (String.IsNullOrEmpty(_baseUrl))
            {
                _logger.Write(TraceEventType.Error, "Null or Empty Base URL for ML Model: {0}", _baseUrl);
                throw new Exception(string.Format("Null or Empty Base URL for ML Model: {0}", _baseUrl));
            }
            _logger.Write(TraceEventType.Information, "Base ML Azure Website url is : {0}", _baseUrl);

            _apiKey = inputs["apiKey"];
            if (String.IsNullOrEmpty(_apiKey))
            {
                _logger.Write(TraceEventType.Error, "Null or Empty API Key for ML Model: {0}", _apiKey);
                throw new Exception(string.Format("Null or Empty API Key for ML Model: {0}", _apiKey));
            }

            // Get the ADF Input Tables
            foreach (var inputTable in inputTables)
            {
                _storageConnectionString = GetConnectionString(inputTable.LinkedService);
                string folderPath = GetFolderPath(inputTable.Table);

                if (String.IsNullOrEmpty(_storageConnectionString))
                {
                    _logger.Write(TraceEventType.Error, "Null or Empty Connection string for input table: {0}", inputTable);
                    throw new Exception(string.Format("Null or Empty Connection string for input table: {0}", inputTable));
                }
                if (String.IsNullOrEmpty(folderPath))
                {
                    _logger.Write(TraceEventType.Error, "Null or Empty folderpath for input table: {0}", inputTable);
                    throw new Exception(string.Format("Null or Empty folder path for input table: {0}", inputTable));
                }
                _storageContainerName = folderPath.Split('/')[0];
                _inputBlobName = folderPath.Substring(folderPath.IndexOf('/') + 1) ;
                _logger.Write(TraceEventType.Information, "Folder Path for Input Table {0}: {1}", inputTable, folderPath);
            }

            // Get the ADF Output Tables
            foreach (var outputTable in outputTables)
            {
                _storageConnectionString = GetConnectionString(outputTable.LinkedService);
                string folderPath = GetFolderPath(outputTable.Table);

                if (String.IsNullOrEmpty(_storageConnectionString))
                {
                    _logger.Write(TraceEventType.Error, "Null or Empty Connection string for output table: {0}", outputTable);
                    throw new Exception(string.Format("Null or Empty Connection string for output table: {0}", outputTable));
                }
                if (String.IsNullOrEmpty(folderPath))
                {
                    _logger.Write(TraceEventType.Error, "Null or Empty folderpath for output table: {0}", outputTable);
                    throw new Exception(string.Format("Null or Empty folder path for output table: {0}", outputTable));
                }
                _outputBlobName = folderPath.Substring(folderPath.IndexOf('/') + 1) ;
                _logger.Write(TraceEventType.Information, "Folder Path for Ouput Table {0}: {1}", outputTable, folderPath);
            }
         
            try
            {
                // Invoke ML Batch Execution Service
                InvokeBatchExecutionService().Wait();
            }
            catch (Exception ex)
            {
                _logger.Write(TraceEventType.Error, "ML Model Call failed with error : {0}", ex.ToString());
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

            _logger.Write(TraceEventType.Information, "Submitting the job...");
            
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
                        _logger.Write(TraceEventType.Information, "Timed out. Deleting the job ...");
                        await client.DeleteAsync(jobLocation);
                    }
                    switch (status.StatusCode)
                    {
                        case BatchScoreStatusCode.NotStarted:
                            _logger.Write(TraceEventType.Information, "Not started...");
                            break;
                        case BatchScoreStatusCode.Running:
                            _logger.Write(TraceEventType.Information, "Running...");
                            break;
                        case BatchScoreStatusCode.Failed:
                            _logger.Write(TraceEventType.Error, "Failed!");
                            _logger.Write(TraceEventType.Error, "Error details : {0}", status.Details);
                            throw new Exception(status.Details);
                        case BatchScoreStatusCode.Cancelled:
                            _logger.Write(TraceEventType.Information, "Cancelled!");
                            throw new Exception(status.Details);
                        case BatchScoreStatusCode.Finished:
                            done = true;
                            _logger.Write(TraceEventType.Information, "Finished!");
                            var credentials = new StorageCredentials(status.Result.SasBlobToken);
                            var sourceCloudBlob = new CloudBlockBlob(new Uri(new Uri(status.Result.BaseLocation), status.Result.RelativeLocation), credentials);
                            var targetCloudBlob = container.GetBlockBlobReference(_outputBlobName);
                            targetCloudBlob.StartCopyFromBlob(sourceCloudBlob);
                            _logger.Write(TraceEventType.Information, "Copy to Output Blob Complete...");
                            break;
                    }

                    if (!done)
                    {
                        Thread.Sleep(1000); // Wait one second
                    }
                }
            }
        }

        // Get Linked Service Connection String
        private static string GetConnectionString(LinkedService asset)
        {
            if (asset == null)
            {
                return null;
            }

            AzureStorageLinkedService storageAsset = asset.Properties as AzureStorageLinkedService;
            if (storageAsset == null)
            {
                return null;
            }

            return storageAsset.ConnectionString;
        }

        // Get Folder Path for table
        private static string GetFolderPath(Table table)
        {
            if (table == null || table.Properties == null)
            {
                return null;
            }

            AzureBlobLocation blobLocation = table.Properties.Location as AzureBlobLocation;
            if (blobLocation == null)
            {
                return null;
            }

            return blobLocation.FolderPath + blobLocation.FileName;
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
