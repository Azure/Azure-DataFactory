using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAzure.Storage;

namespace ADF.Sample.ExecuteRScriptWithCustomActivity
{
    public class RExecutionActivity : IDotNetActivity
    {
        /// <summary>
        /// Override Execute method in ADF .Net Activity
        /// </summary>
        /// <param name="linkedServices"></param>
        /// <param name="datasets"></param>
        /// <param name="activity"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public IDictionary<string, string> Execute(IEnumerable<LinkedService> linkedServices, IEnumerable<Dataset> datasets, Activity activity, IActivityLogger logger)
        {
            logger.Write("Executing R with ADF .Net Activity");

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            IDictionary<string, string> extendedProperties = ((DotNetActivity)activity.TypeProperties).ExtendedProperties;

            //These file paths are specific to the R script.  We pass the file paths to our R script as parameters via the pipeline json
            string experimentType;
            extendedProperties.TryGetValue("modelType", out experimentType);
            string snapShotFile;
            extendedProperties.TryGetValue("snapShotFile", out snapShotFile);
            string timeSeriesFile;
            extendedProperties.TryGetValue("timeSeriesFile", out timeSeriesFile);
            string blobPath;
            extendedProperties.TryGetValue("blobPath", out blobPath);
            string churnTagFile;
            extendedProperties.TryGetValue("churnTagFile", out churnTagFile);
            string outputFile;
            extendedProperties.TryGetValue("outputFile", out outputFile);

            logger.Write("Starting Batch Execution Service");

            InvokeR(experimentType, snapShotFile, timeSeriesFile, churnTagFile, blobPath, outputFile, logger);

            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Resolve local path on HDInsight VM where the ADF .Net activity is running
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyname = new AssemblyName(args.Name).Name;
            if (assemblyname.Contains("Microsoft.WindowsAzure.Storage"))
            {
                assemblyname = "Microsoft.WindowsAzure.Storage";
                var assemblyFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assemblyname + ".dll");
                var assembly = Assembly.LoadFrom(assemblyFileName);
                return assembly;
            }
            return null;
        }

        /// <summary>
        /// Invoke RScript.exe and run the R script
        /// </summary>
        /// <param name="experimentType"></param>
        /// <param name="snapShotFile"></param>
        /// <param name="timeSeriesFile"></param>
        /// <param name="churnTagFile"></param>
        /// <param name="blobPath"></param>
        /// <param name="outputFile"></param>
        /// <param name="logger"></param>
        public static void InvokeR(string experimentType, string snapShotFile, string timeSeriesFile, string churnTagFile, string blobPath, string outputFile, IActivityLogger logger)
        {

            const string accountName = "<Provide Storage Account Name>";
            const string accountKey = "<Provide Storage Account Key>";
            var connectionString = String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", accountName, accountKey);

            var process = new Process();

            try
            {
                string pathToRExecutable;
                string[] blobNames;
                const string containerName = "hdiclustertest";

                //gen_ts_features.r is one of the files the R script requires. train.r and score.r are the R scripts themselves.
                var sourceFile = "gen_ts_features.r";
                if (experimentType.ToUpper().Equals("TRAINING"))
                {
                    pathToRExecutable = "train.r";
                    blobNames = new[] { sourceFile, pathToRExecutable, snapShotFile, timeSeriesFile, churnTagFile };
                }
                else
                {
                    pathToRExecutable = "score.r";
                    blobNames = new[] { sourceFile, pathToRExecutable, snapShotFile, timeSeriesFile };
                }

                var resultBlobPath = String.Format("{0}/{1}", containerName, blobPath);

                logger.Write("Creating working directory");
                logger.Write(String.Format("Machine Name: {0}", Environment.MachineName));

                var workingDirectory = new FileInfo(typeof(RExecutionActivity).Assembly.Location).DirectoryName;
                logger.Write(String.Format("Directory Name : {0}", workingDirectory));

                //var workingDirectory = CreateWorkingDirectory();
                logger.Write(String.Format("Working directory created: {0}", workingDirectory));

                DirectoryCopy(@"C:\apps\dist\R", workingDirectory, true);

                logger.Write("Downloading input files used by this sample to the Working Directory");

                var inputFileNames = DownloadInputFiles(workingDirectory, connectionString, resultBlobPath, blobNames);

                var index = 0;
                for (; index < inputFileNames.Length; index++)
                {
                    var file = inputFileNames[index];
                    if (File.Exists(file))
                    {
                        logger.Write(String.Format("File : {0} exists", file));
                    }
                }

                logger.Write("Input Files Download completed");

                sourceFile = inputFileNames[0];
                pathToRExecutable = inputFileNames[1];
                var ssFile = inputFileNames[2];
                var tsFile = inputFileNames[3];
                string args;
                var outputFileName = String.Format("{0}\\{1}", workingDirectory, outputFile);

                logger.Write(String.Format("Output file name : {0}", outputFileName));
                
                if (experimentType.ToUpper().Equals("TRAINING"))
                {
                    var tagFile = inputFileNames[4];
                    args = String.Format("{0} {1} {2} {3} {4}", sourceFile, tsFile, ssFile, tagFile, outputFileName);
                    logger.Write(String.Format("Arguments in training are : {0}", args));
                }
                else
                {
                    args = String.Format("{0} {1} {2} {3}", sourceFile, tsFile, ssFile, outputFileName);
                }

                /////R execution/////

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                logger.Write(File.Exists(String.Format("{0}{1}", workingDirectory, @"\R-3.2.2\bin\x64\Rscript.exe"))
                    ? "R File exists"
                    : "R file does not exist");

                startInfo.FileName = String.Format("{0}{1}", workingDirectory, @"\R-3.2.2\bin\x64\Rscript.exe");
                startInfo.Arguments = String.Format("{0} {1}", pathToRExecutable, args);
                if (workingDirectory != null) startInfo.WorkingDirectory = workingDirectory;
                logger.Write("R Execution started");
                process.StartInfo = startInfo;
                process.Start();

                logger.Write(String.Format("Process started with process id : {0} on machine : {1}", process.Id, process.MachineName));

                var errorReader = process.StandardError;
                var outputReader = process.StandardOutput;
                
                while (!outputReader.EndOfStream)
                {
                    var text = outputReader.ReadLine();
                    logger.Write(text);
                }

                logger.Write("output reader complete");

                while (!errorReader.EndOfStream)
                {
                    errorReader.ReadLine();
                }

                logger.Write(String.Format("Standard Output : {0}", process.StandardOutput.ReadToEnd()));
                logger.Write(String.Format("Standard Error: {0}", process.StandardError.ReadToEnd()));

                logger.Write("output reader end of stream complete");

                process.WaitForExit();

                while (!process.HasExited)
                {
                    logger.Write("R is still running");
                }

                logger.Write(String.Format("Process start time : {0}, end time : {1}", process.StartTime, process.ExitTime));

                /////Upload file/////
                if (File.Exists(outputFileName))
                {
                    logger.Write("Uploading file started");

                    UploadFile(connectionString, resultBlobPath, outputFileName, outputFile);
                }
                else
                {
                    logger.Write("output file not found");
                }
            }
            catch (Exception ex)
            {
                logger.Write(String.Format("Exception is : {0}", ex.Message));
            }

        }

        /// <summary>
        /// Upload file to  Azure Blob
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="containerName"></param>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        private static void UploadFile(string connectionString, string containerName, string filePath, string fileName)
        {
            var storageAccount =
                CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(fileName);
            blob.UploadFromFile(filePath, FileMode.Open);
            Console.WriteLine("File upload completed");
        }

        /// <summary>
        /// Download input files from Azure blob
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="connectionString"></param>
        /// <param name="containerName"></param>
        /// <param name="blobNames"></param>
        /// <returns></returns>
        private static string[] DownloadInputFiles(string workingDirectory, string connectionString,
            string containerName, string[] blobNames)
        {
            var inputStorageAccount =
                CloudStorageAccount.Parse(connectionString);
            var inputClient = inputStorageAccount.CreateCloudBlobClient();
            var container = inputClient.GetContainerReference(containerName);
            var inputFiles = new string[blobNames.Length];

            for (var blobCnt = 0; blobCnt < blobNames.Length; blobCnt++)
            {
                var blobName = blobNames[blobCnt];
                var blockBlob =
                    container.GetBlockBlobReference(blobName);

                using (var fileStream =
                    File.OpenWrite(Path.Combine(workingDirectory, blockBlob.Name)))
                {
                    blockBlob.DownloadToStream(fileStream);

                    inputFiles[blobCnt] = Path.Combine(workingDirectory, blockBlob.Name);
                }
            }

            return inputFiles;
        }

        /// <summary>
        /// Copy files from source to destination directory recursively
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            var dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (!copySubDirs) return;
            foreach (var subdir in dirs)
            {
                var temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, true);
            }
        }

    }
}
