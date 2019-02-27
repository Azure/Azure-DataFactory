namespace DataDownloaderActivityNS
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using Microsoft.Azure.Management.DataFactories.Models;
    using Microsoft.Azure.Management.DataFactories.Runtime;

    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;

    public class DataDownloaderActivity : IDotNetActivity
    {
        private IActivityLogger _logger;
        private string _dataStorageAccountName;
        private string _dataStorageAccountKey;
        private string _dataStorageContainer;

        //public IDictionary<string, string> Execute(
        //    IEnumerable<ResolvedTable> inputTables,
        //    IEnumerable<ResolvedTable> outputTables,
        //    IDictionary<string, string> inputs,
        //    IActivityLogger activityLogger)
        public IDictionary<string, string> Execute(
            IEnumerable<LinkedService> linkedServices,
            IEnumerable<Dataset> datasets,
            Activity activity,
            IActivityLogger logger)
        {

            // to get extended properties (for example: SliceStart)
            DotNetActivity dotNetActivity = (DotNetActivity)activity.TypeProperties;
            _dataStorageAccountName = dotNetActivity.ExtendedProperties["dataStorageAccountName"];
            _dataStorageAccountKey = dotNetActivity.ExtendedProperties["dataStorageAccountKey"];
            _dataStorageContainer = dotNetActivity.ExtendedProperties["dataStorageContainer"];
            string sliceStartTime = dotNetActivity.ExtendedProperties["sliceStart"];
            string urlFormat = dotNetActivity.ExtendedProperties["urlFormat"];

            _logger = logger;
            GatherDataForOneHour(sliceStartTime, urlFormat);

            _logger.Write("Exit");
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

            _logger.Write("Current data slot gathered : {0}.......", dataSlotGathered);

            // Temporary staging folder
            string dataStagingFolder = string.Format(@"{0}\{1}\{1}-{2}", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), year, month);
            Directory.CreateDirectory(dataStagingFolder);

            // Temporary staging file
            string hourlyFileName = string.Format("data-{0}{1}{2}-{3}0000.txt", year, month, day, hour);
            string decompressedFile = Path.Combine(dataStagingFolder, hourlyFileName);

            try
            {
                _logger.Write("Gathering hourly data: ..");
                TriggerRequest(urlFormat, year, month, day, hour, decompressedFile);

                _logger.Write("Uploading to Blob: ..");
                CloudBlobClient blobClient = new CloudBlobClient(storageAccountUri, new StorageCredentials(_dataStorageAccountName, _dataStorageAccountKey));
                string blobPath = string.Format(CultureInfo.InvariantCulture, "httpdownloaddatain/{0}-{1}-{2}-{3}/{4}",
                    year, month, day, hour, hourlyFileName);

                CloudBlobContainer container = blobClient.GetContainerReference(_dataStorageContainer);
                container.CreateIfNotExists();

                var blob = container.GetBlockBlobReference(blobPath);
                blob.UploadFromFile(decompressedFile, FileMode.OpenOrCreate);
            }
            catch (Exception ex)
            {
                _logger.Write("Error occurred : {0}", ex);
                throw;
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
            int retries = 0;
            bool found = false;
            while (retries <= 10 && !found)
            {
                string url = string.Format(urlFormat, year, month, day, hour, retries.ToString("00"));
                try
                {
                    _logger.Write("Making request to url : {0}..", url);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            _logger.Write("Decompressing to a file: ..");
                            using (FileStream decompressedFileStream = File.Create(decompressedFile))
                            {
                                using (GZipStream decompressionStream = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
                                {
                                    decompressionStream.CopyTo(decompressedFileStream);
                                    _logger.Write("Decompression complete to : {0}", decompressedFile);
                                }
                            }
                        }
                    }
                    found = true;
                }
                catch (Exception e)
                {
                    _logger.Write("Unable to download : {0} with error: {1}.", url, e.Message);
                    if (retries == 10)
                    {
                        throw;
                    }
                }
                retries++;
                Thread.Sleep(1000);
            }
        }
    }
}
