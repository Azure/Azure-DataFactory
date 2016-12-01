using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.Azure.Management.DataFactories.Runtime;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ADF.Sample.DeleteFromBlobCustomActivity
{
    public class DeleteFromBlobActivity : IDotNetActivity
    {
        public IDictionary<string, string> Execute(IEnumerable<LinkedService> linkedServices,
                                                   IEnumerable<Dataset> datasets,
                                                   Activity activity,
                                                   IActivityLogger logger)
        {
            try
            {
                logger.Write("Custom Activity Started.");

                DotNetActivity dotNetActivity = (DotNetActivity)activity.TypeProperties;
                string inputToDelete = dotNetActivity.ExtendedProperties["InputToDelete"];
                logger.Write("\nInput to delete is " + inputToDelete);

                logger.Write("\nAll Dataset(s) Below " );
                foreach (Dataset ds in datasets)
                {
                    logger.Write("\nDataset: " + ds.Name);
                }

                foreach (string name in activity.Inputs.Select(i => i.Name))
                {
                    logger.Write("\nInput Dataset: " + name);
                }

                foreach (string name in activity.Outputs.Select(i => i.Name))
                {
                    logger.Write("\nOutput Dataset: " + name);
                }

                List<string> dataSetsToDelete = inputToDelete.Split(',').ToList();

                DeleteBlobFileFolder(dataSetsToDelete);

                logger.Write("Custom Activity Ended Successfully.");
            }
            catch (Exception e)
            {
                logger.Write("Custom Activity Failed with error.");
                logger.Write("Caught exception: ");
                logger.Write(e.Message);
                throw new Exception(e.Message);
            }

            // The dictionary can be used to chain custom activities together in the future.
            // This feature is not implemented yet, so just return an empty dictionary.
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Delete azure blob file or entire folder
        /// </summary>
        /// <param name="dataSetsToDelete"></param>
        /// 
        public void DeleteBlobFileFolder(List<string> dataSetsToDelete)
        {
            foreach (string strInputToDelete in dataSetsToDelete)
            {
                Dataset inputDataset = datasets.First(ds => ds.Name.Equals(strInputToDelete));
                AzureBlobDataset blobDataset = inputDataset.Properties.TypeProperties as AzureBlobDataset;
                logger.Write("\nBlob folder: " + blobDataset.FolderPath);
                logger.Write("\nBlob file: " + blobDataset.FileName);

                // linked service for input and output is the same.
                AzureStorageLinkedService linkedService = linkedServices.First(ls =>
                    ls.Name == inputDataset.Properties.LinkedServiceName).Properties.TypeProperties as AzureStorageLinkedService;

                // create storage client for input. Pass the connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(linkedService.ConnectionString);
                CloudBlobClient client = storageAccount.CreateCloudBlobClient();

                // find blob to delete and delete if exists.
                Uri blobUri = new Uri(storageAccount.BlobEndpoint, blobDataset.FolderPath + blobDataset.FileName);
                CloudBlockBlob blob = new CloudBlockBlob(blobUri, storageAccount.Credentials);
                logger.Write("Blob Uri: {0}", blobUri.AbsoluteUri);
                logger.Write("Blob exists: {0}", blob.Exists());
                blob.DeleteIfExists();
                logger.Write("Deleted blob: {0}", blobUri.AbsoluteUri);

                // Ensure the container is exist.
                if (blobDataset.FolderPath.IndexOf("/") > 0)
                {
                    string containerName = blobDataset.FolderPath.Substring(0, blobDataset.FolderPath.IndexOf("/"));
                    logger.Write("Container Name {0}", containerName);

                    string directoryName = blobDataset.FolderPath.Substring(blobDataset.FolderPath.IndexOf("/") + 1);
                    logger.Write("Directory Name {0}", directoryName);

                    var blobContainer = client.GetContainerReference(containerName);
                    blobContainer.CreateIfNotExists();
                    CloudBlobDirectory cbd = blobContainer.GetDirectoryReference(directoryName);

                    foreach (IListBlobItem item in blobContainer.ListBlobs(directoryName, true))
                    {
                        logger.Write("Blob Uri: {0} ", item.Uri.AbsoluteUri);

                        if (item.GetType() == typeof(CloudBlockBlob) || item.GetType().BaseType == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob subBlob = new CloudBlockBlob(item.Uri, storageAccount.Credentials);
                            logger.Write("Blob exists: {0}", subBlob.Exists());
                            subBlob.DeleteIfExists();
                            logger.Write("Deleted blob {0}", item.Uri.AbsoluteUri);
                        }
                    }
                }
            }
        }
    }
}
