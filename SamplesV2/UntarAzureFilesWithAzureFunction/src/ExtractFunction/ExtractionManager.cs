using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.DataMovement;
using Microsoft.WindowsAzure.Storage.File;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace ExtractFunction
{
    public class ExtractionManager
    {
        public static async Task<dynamic> ExtractAndUploadFiles(ILogger log, CloudFile sourceFileReference,
            CloudFileDirectory destinationDirectory)
        {
            dynamic returnObject = new ExpandoObject();
            returnObject.Output = new List<string>();

            string archivedFilePath = GetTempFileName();
            IArchive archive = null;
            try
            {
                await TransferManager.DownloadAsync(sourceFileReference, archivedFilePath,
                    new DownloadOptions { DisableContentMD5Validation = true }, new SingleTransferContext());

                archive = ArchiveFactory.Open(archivedFilePath);
                IReader reader = archive.ExtractAllEntries();

                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.IsDirectory) continue;

                    //The file is a compressed archive container. The library will only decompress the stream to an archive. We need to reprocess the archive to get the contents.
                    if (reader.Entry.Key == null)
                    {
                        string tempArchivedFilePath = GetTempFileName();
                        reader.WriteEntryToFile(tempArchivedFilePath);

                        archive.Dispose();
                        File.Delete(archivedFilePath);

                        archivedFilePath = tempArchivedFilePath;
                        archive = ArchiveFactory.Open(archivedFilePath);
                        reader = archive.ExtractAllEntries();
                        if (!reader.MoveToNextEntry())
                            break;
                    }

                    await UploadFile(destinationDirectory, reader, returnObject);
                }
            }
            finally
            {
                archive?.Dispose();
                File.Delete(archivedFilePath);
            }


            return returnObject;
        }

        private static async Task UploadFile(CloudFileDirectory destinationFileDirectory, IReader reader, dynamic returnObject)
        {
            CloudFile destinationFileReference = destinationFileDirectory.GetFileReference(reader.Entry.Key);
            await destinationFileReference.DeleteIfExistsAsync();
            string tempFileName = GetTempFileName();

            try
            {
                reader.WriteEntryToFile(tempFileName);
                await TransferManager.UploadAsync(tempFileName, destinationFileReference);
            }
            finally
            {
                File.Delete(tempFileName);

            }


            returnObject.Output.Add(destinationFileReference.Uri.ToString());
        }

        private static string GetTempFileName()
        {
            return Path.GetTempPath() + $"{Guid.NewGuid()}.tmp";
        }
    }
}