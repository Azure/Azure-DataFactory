using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.DataFactories.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CustomActivityRunner
{
    public class BlobUtilities
    {
        private IActivityLogger logger;
        private string connectionString;
        private string folderPath;

        public BlobUtilities(IActivityLogger logger, string connectionString, string folderPath)
        {
            this.logger = logger;
            this.connectionString = connectionString;
            this.folderPath = folderPath;
        }

        public string DownLoadFile(string blobFileName)
        {
            logger.Write($"Downloading file '{blobFileName}' from blob '{folderPath}'");

            var blockBlob = GetCloudBlockBlob(blobFileName);
            string localFile = Path.Combine(Path.GetTempPath(), blobFileName);

            if (!blockBlob.Exists())
            {
                return null;
            }

            blockBlob.DownloadToFile(localFile, FileMode.Create);

            return localFile;
        }

        public string DownLoadLatestFile()
        {
            CloudBlockBlob latestBlob = GetLatestBlob();

            if (latestBlob == null)
                return null;

            string filename;

            // If the blob is embedded in a folder heirarchy find the name of the actual file
            if(latestBlob.Name.Contains("/"))
                filename = latestBlob.Name.Substring(latestBlob.Name.LastIndexOf('/')+1);
            else
                filename = latestBlob.Name;


            string localFile = Path.Combine(GetTemporaryDirectory(), filename);

            logger.Write($"Downloading latest blob file '{latestBlob.Name}' from blob folder '{folderPath}'");

            latestBlob.DownloadToFile(localFile, FileMode.Create);

            return localFile;
        }

        public void UploadFile(string localFilePath)
        {
            logger.Write($"Uploading file '{localFilePath}' to blob '{folderPath}'");

            var blockBlob = GetCloudBlockBlob(Path.GetFileName(localFilePath), true);
            blockBlob.UploadFromFile(localFilePath, FileMode.Open);
        }

        public async Task<string> DownLoadFileAsync(string fileName)
        {
            var blockBlob = GetCloudBlockBlob(fileName);
            string localFile = Path.Combine(Path.GetTempPath(), fileName);
            await blockBlob.DownloadToFileAsync(localFile, FileMode.Create);

            return localFile;
        }

        public string GetLatestBlobFileName()
        {
            CloudBlockBlob latestBlob = GetLatestBlob();

            return Path.GetFileNameWithoutExtension(latestBlob?.Name);
        }

        public void RemoveFilesFromBlob(string fileName = null)
        {
            var container = GetCloudBlobContainer();
            
            var blobs = container.ListBlobs();

            // Optionally delete just a single file
            if (fileName != null)
            {
                blobs = container.ListBlobs().Where(x => x.Uri.ToString().Contains(fileName));
            }

            foreach (CloudBlockBlob blob in blobs)
            {
                logger.Write($"Removing blob '{blob.Name}' from storage '{folderPath}'");
                blob.DeleteIfExists();
            }
        }


        private CloudBlockBlob GetCloudBlockBlob(string fileName, bool createContainer = false)
        {
            var container = GetCloudBlobContainer();

            if (createContainer)
            {
                //Create a new container, if it does not exist
                container.CreateIfNotExists();
            }

            // Get the block blob
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

            return blockBlob;
        }

        private CloudBlobContainer GetCloudBlobContainer(string inputPath = null)
        {
            var path = inputPath ?? folderPath;

            CloudStorageAccount inputStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = inputStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(path);
            return container;
        }

        public CloudBlockBlob GetLatestBlob()
        {
            var container = GetCloudBlobContainer();
            var blobs = container.ListBlobs(useFlatBlobListing:true);


            var listBlobItems = blobs as IList<IListBlobItem> ?? blobs.ToList();
            CloudBlockBlob latestBlob = !listBlobItems.Any() ? null :
                listBlobItems.OrderByDescending(x => ((CloudBlockBlob)x).Properties.LastModified).First() as CloudBlockBlob;

            return latestBlob;
        }

        private string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public void ArchiveFile(string archiveContainer, string path)
        {
            var blobName = DateTime.Now.ToString("s").ToLower() + ": " + Path.GetFileName(path).ToLower();

            CloudStorageAccount inputStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = inputStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(archiveContainer);


            var cloudBlock = container.GetBlockBlobReference(blobName);           

            using (var fileStream = File.OpenRead(path))
            {
                cloudBlock.UploadFromStream(fileStream);                
            }
        }

        public void CopyToNewBlobLocation(string storageConnectionString, string filePath, string folderName)
        {
            var blobName = Path.GetFileName(filePath).ToLower();
            
            CloudStorageAccount inputStorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = inputStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(folderName);

            var cloudBlock = container.GetBlockBlobReference(blobName);

            using (var fileStream = File.OpenRead(filePath))
            {
                cloudBlock.UploadFromStream(fileStream);
            }
        }
    }
}
