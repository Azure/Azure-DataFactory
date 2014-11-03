using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading;
using Microsoft.DataFactories.Runtime;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace CustomDataDownloader
{
    public class DataDownloader : ICustomActivity
    {
        private IActivityLogger _logger;
        private string _dataStorageAccountName;
        private string _dataStorageAccountKey;
        private string _dataStorageContainer;

        public IDictionary<string, string> Execute(
            IEnumerable<ResolvedTable> inputTables,
            IEnumerable<ResolvedTable> outputTables,
            IDictionary<string, string> inputs,
            IActivityLogger activityLogger)
        {
            _dataStorageAccountName = inputs["dataStorageAccountName"];
            _dataStorageAccountKey = inputs["dataStorageAccountKey"];
            _dataStorageContainer = inputs["dataStorageContainer"];
            string sliceStartTime = inputs["sliceStart"];
            string urlFormat = inputs["urlFormat"];
            _logger = activityLogger;
            _logger.Write(TraceEventType.Information, "Data Storage Account Name is : {0}", _dataStorageAccountName);
            _logger.Write(TraceEventType.Information, "Data Storage Account Name is : {0}", _dataStorageAccountKey);
            _logger.Write(TraceEventType.Information, "URL Format is : {0}", urlFormat);
            _logger.Write(TraceEventType.Information, "Slice start time is : {0}", sliceStartTime);
            GatherDataForOneHour(sliceStartTime, urlFormat);

            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Gather data for each Hour based on Slice Start Time.
        /// </summary>
        /// <param name="sliceStartTime"></param>
        /// <param name="urlFormat"></param>
        private void GatherDataForOneHour(string sliceStartTime, string urlFormat)
        {
            Uri storageAccountUri = new Uri("http://" + _dataStorageAccountName + ".blob.core.windows.net/");
            string year = sliceStartTime.Substring(0, 4);
            string month = sliceStartTime.Substring(4, 2);
            string day = sliceStartTime.Substring(6, 2);
            string hour = sliceStartTime.Substring(8, 2);
            string minute = sliceStartTime.Substring(10, 2);
            DateTime dataSlotGathered = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day), int.Parse(hour), int.Parse(minute), 0);

            _logger.Write(TraceEventType.Information, "Current data slot gathered : {0}.......", dataSlotGathered);

            // Temporary staging folder
            string dataStagingFolder = string.Format(@"{0}\{1}\{1}-{2}", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), year, month);
            Directory.CreateDirectory(dataStagingFolder);

            // Temporary staging file
            string hourlyFileName = string.Format("data-{0}{1}{2}-{3}0000.txt", year, month, day, hour);
            string decompressedFile = Path.Combine(dataStagingFolder, hourlyFileName);

            try
            {
                _logger.Write(TraceEventType.Information, "Gathering hourly data: ..");
                TriggerRequest(urlFormat, year, month, day, hour, decompressedFile);

                _logger.Write(TraceEventType.Information, "Uploading to Blob: ..");
                CloudBlobClient blobClient = new CloudBlobClient(storageAccountUri, new StorageCredentialsAccountAndKey(_dataStorageAccountName, _dataStorageAccountKey));
                string blobPath = string.Format(CultureInfo.InvariantCulture, "httpdownloaddatain/{0}-{1}-{2}-{3}/{4}",
                    year, month, day, hour, hourlyFileName);

                CloudBlobContainer container = blobClient.GetContainerReference(_dataStorageContainer);
                container.CreateIfNotExist();

                CloudBlob blob = container.GetBlobReference(blobPath);

                BlobRequestOptions options = new BlobRequestOptions
                {
                    Timeout = new TimeSpan(0, 5, 0),
                    RetryPolicy = RetryPolicies.Retry(3, new TimeSpan(0, 1, 0))
                };
                blob.UploadFile(decompressedFile, options);
            }
            catch (Exception ex)
            {
                _logger.Write(TraceEventType.Error, "Error occurred : {0}", ex);
            }
            finally
            {
                if (File.Exists(decompressedFile))
                {
                    File.Delete(decompressedFile);
                }
            }
        }

        /// <summary>
        /// Trigger request to the HTTP Endpoint
        /// </summary>
        /// <param name="urlFormat"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="decompressedFile"></param>
        private void TriggerRequest(string urlFormat, string year, string month, string day, string hour, string decompressedFile)
        {
            int retries = 1;
            bool found = false;
            while (retries <= 10 && !found)
            {
                try
                {
                    string url = string.Format(urlFormat, year, month, day, hour, retries.ToString("00"));
                    _logger.Write(TraceEventType.Information, "Making request to url : {0}..", url);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            _logger.Write(TraceEventType.Information, "Decompressing to a file: ..");
                            using (FileStream decompressedFileStream = File.Create(decompressedFile))
                            {
                                using (GZipStream decompressionStream = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
                                {
                                    decompressionStream.CopyTo(decompressedFileStream);
                                    _logger.Write(TraceEventType.Information, "Decompression complete to : {0}", decompressedFile);
                                }
                            }
                        }
                    }
                    found = true;
                }
                catch
                {
                    if (retries == 10)
                    {
                        throw;
                    }
                }
                retries++;
                Thread.Sleep(5000);
            }
        }
    }
}
