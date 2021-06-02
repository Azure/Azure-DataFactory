using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace V2CustomExeSample
{
    class Listall   
    {
        /// <summary>
        /// This sample executable is to demonstrate how custom DLL you wrote for the ADFv1 .NET Custom Activity 
        /// can be rewritten to a custom executable file to be executed by ADFv2 Custom Activity. 
        /// </summary>
        
        static void Main(string[] args)
        {

            Console.WriteLine("Start to execute custom activity V2");

            // Parse activity and reference objects info from input files
            dynamic activity = JsonConvert.DeserializeObject(File.ReadAllText("activity.json"));
            dynamic linkedServices = JsonConvert.DeserializeObject(File.ReadAllText("linkedServices.json"));

            // Extract Connection String from LinkedService
            dynamic storageLinkedService = ((JArray)linkedServices).First(_ => "BatchStorageLinkedService".Equals(((dynamic)_).name.ToString()));
            string connectionString = storageLinkedService.properties.typeProperties.connectionString.value;

            // Extract InputFilePath & OutputFilePath from ExtendedProperties
            // In ADFv2, Input & Output Datasets are not required for Custom Activity. In this sample the folderName and 
            // fileName properties are stored in ExtendedProperty of the Custom Activity like below. You are not required
            // to get the information from Datasets. 

            //"extendedProperties": {
            //    "InputFolderPath": "batchjobs/filestocheck",
            //            "OutputFilePath": "batchjobs/filestocheck/outputfile.txt"
            //        }                
            string inputFolderPath = activity.typeProperties.extendedProperties.InputFolderPath;
            string outputFilePath = activity.typeProperties.extendedProperties.OutputFilePath;
            //V1 Logger is no longer required as your executable can directly write to STDOUT
            Console.WriteLine(string.Format("InputFilePath: {0}, OutputFilePath: {1}", inputFolderPath, outputFilePath));

            // Extract Input & Output Dataset
            // If you would like to continue using Datasets, pass the Datasets in referenceObjects of the Custom Activity JSON payload like below: 

            //"referenceObjects": {
            //    "linkedServices": [
            //                {
            //                    "referenceName": "BatchStorageLinkedService",
            //                    "type": "LinkedServiceReference"
            //                }
            //            ],
            //            "datasets": [
            //                {
            //                    "referenceName": "InputDataset",
            //                    "type": "DatasetReference"
            //                },
            //                {
            //                    "referenceName": "OutputDataset",
            //                    "type": "DatasetReference"
            //                }
            //            ]
            //        }

            // Then you can use following code to get the folder and file info instead:  
            //dynamic datasets = JsonConvert.DeserializeObject(File.ReadAllText("datasets.json"));
            //dynamic inputDataset = ((JArray)datasets).First(_ => ((dynamic)_).name.ToString().StartsWith("InputDataset"));
            //dynamic outputDataset = ((JArray)datasets).First(_ => ((dynamic)_).name.ToString().StartsWith("OutputDataset"));
            //string inputFolderPath = inputDataset.properties.typeProperties.folderPath; 
            //string outputFolderPath = outputDataset.properties.typeProperties.folderPath; 
            //string outputFile = outputDataset.properties.typeProperties.fileName;
            //string outputFilePath = outputFolderPath + "/" + outputFile; 


            //Once needed info is prepared, core business logic down below remains the same. 

            string output = string.Empty; // for use later.

            // create storage client for input. Pass the connection string.
            CloudStorageAccount inputStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient inputClient = inputStorageAccount.CreateCloudBlobClient();

            // initialize the continuation token before using it in the do-while loop.
            BlobContinuationToken continuationToken = null;
            do
            {   // get the list of input blobs from the input storage client object.
                BlobResultSegment blobList = inputClient.ListBlobsSegmented(inputFolderPath,
                                         true,
                                         BlobListingDetails.Metadata,
                                         null,
                                         continuationToken,
                                         null,
                                         null);

                // Calculate method returns the number of occurrences of
                // the search term (“Microsoft”) in each blob associated
                // with the data slice. definition of the method is shown in the next step.

                output = Calculate(blobList, inputFolderPath, ref continuationToken, "Microsoft");

            } while (continuationToken != null);

            CloudStorageAccount outputStorageAccount = CloudStorageAccount.Parse(connectionString);
            // write the name of the file.
            Uri outputBlobUri = new Uri(outputStorageAccount.BlobEndpoint, outputFilePath);

            // log the output file name
            Console.WriteLine("output blob URI: {0}", outputBlobUri.ToString());

            // create a blob and upload the output text.
            CloudBlockBlob outputBlob = new CloudBlockBlob(outputBlobUri, outputStorageAccount.Credentials);
            Console.WriteLine("Writing {0} to the output blob", output);
            outputBlob.UploadText(output);
        }

        public static string Calculate(BlobResultSegment Bresult, string folderPath, ref BlobContinuationToken token, string searchTerm)
        {
            string output = string.Empty;
            Console.WriteLine("number of blobs found: {0}", Bresult.Results.Count<IListBlobItem>());
            foreach (IListBlobItem listBlobItem in Bresult.Results)
            {
                CloudBlockBlob inputBlob = listBlobItem as CloudBlockBlob;
                if ((inputBlob != null) && (inputBlob.Name.IndexOf("$$$.$$$") == -1))
                {
                    string blobText = inputBlob.DownloadText(Encoding.ASCII, null, null, null);
                    Console.WriteLine("input blob text: {0}", blobText);
                    string[] source = blobText.Split(new char[] { '.', '?', '!', ' ', ';', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var matchQuery = from word in source
                                     where word.ToLowerInvariant() == searchTerm.ToLowerInvariant()
                                     select word;
                    int wordCount = matchQuery.Count();
                    output += string.Format("{0} occurrences(s) of the search term \"{1}\" were found in the file {2}.\r\n", wordCount, searchTerm, inputBlob.Name);
                }
            }
            return output;
        }
    }
}
