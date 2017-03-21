using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public class BlobUtilities : IBlobUtilities
    {
        private ILogger logger;

        public BlobUtilities(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Uploads the file to blob storage.
        /// </summary>
        /// <param name="localFilePath">The local file path.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="folderPath">The folder path.</param>
        public async Task<bool> UploadFile(string localFilePath, string connectionString, string folderPath)
        {
            bool result = true;

            try
            {
                var blockBlob = GetCloudBlockBlob(Path.GetFileName(localFilePath), connectionString, folderPath);
                await blockBlob.UploadFromFileAsync(localFilePath, FileMode.Open);

                logger.Write($"'{localFilePath}' uploaded to blob sucessfully", "Green");
            }
            catch (Exception e)
            {
                logger.Write($"Failed to upload {localFilePath} to blob. Error: {e.Message}", "Red");
                logger.WriteError(e);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Gets the cloud block blob.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="folderPath">The folder path.</param>
        public CloudBlockBlob GetCloudBlockBlob(string fileName, string connectionString, string folderPath)
        {
            var container = GetCloudBlobContainer(connectionString, folderPath);

            //Create a new container, if it does not exist
            container.CreateIfNotExists();

            // Get the block blob
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

            return blockBlob;
        }

        /// <summary>
        /// Gets the cloud blob container.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="folderPath">The folder path.</param>
        private CloudBlobContainer GetCloudBlobContainer(string connectionString, string folderPath)
        {
            CloudStorageAccount inputStorageAccount;
            try
            {
                inputStorageAccount = CloudStorageAccount.Parse(connectionString);
            }
            catch 
            {
                throw new Exception("The connection string is not in the correct format.");
            }

            CloudBlobClient blobClient = inputStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(folderPath);
            return container;
        }
    }
}
