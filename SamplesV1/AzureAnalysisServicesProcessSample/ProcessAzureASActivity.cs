namespace AzureAnalysisServicesProcessSample
{
    using Microsoft.AnalysisServices.Tabular;
    using Microsoft.AnalysisServices.AdomdClient;
    using Microsoft.Azure.Management.DataFactories.Models;
    using Microsoft.Azure.Management.DataFactories.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System.IO;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System.Globalization;



    /// <summary>
    /// Custom activity to process a Tabular model.
    /// </summary>
    public class ProcessAzureASActivity : CrossAppDomainDotNetActivity<ProcessAzureASContext>
    {
        /// <summary>
        /// Names of the parameters used in the Activity JSON.
        /// </summary>
        const string TABULAR_DATABASE_NAME_PARAMETER_NAME = "TabularDatabaseName";
        const string AZUREAS_CONNECTION_STRING_PARAMETER_NAME = "AzureASConnectionString";
        const string ADV_AS_PROCESS_SCRIPT_PATH_PARAMETER_NAME = "AdvancedASProcessingScriptPath";
        const string AZUREAD_AUTHORITY_PARAMETER_NAME = "AzureADAuthority";
        const string AZUREAD_RESOURCE_PARAMETER_NAME = "AzureADResource";
        const string AZUREAD_CLIENTID_PARAMETER_NAME = "AzureADClientId";
        const string AZUREAD_CLIENTSECRETPATH_PARAMETER_NAME = "AzureADClientSecretPath";

        internal override ProcessAzureASContext PreExecute(IEnumerable<LinkedService> linkedServices, IEnumerable<Dataset> datasets, Activity activity, IActivityLogger logger)
        {
            ValidateParameters(linkedServices, datasets, activity, logger);

            return CreateContext(linkedServices, datasets, activity, logger);
        }

        public override IDictionary<string, string> Execute(ProcessAzureASContext context, IActivityLogger logger)
        {
            logger.Write("Starting ProcessAzureASActivity");


            if (string.IsNullOrEmpty(context.AdvancedASProcessingScriptPath))
            {
                logger.Write("No custom TMSL script specified, process perform full process of the database");
                try
                {
                    Model tabularModel = GetTabularModel(context.AzureASConnectionString, context.TabularDatabaseName);

                    ProcessTabularModel(tabularModel, logger);

                    logger.Write("Finalizing ProcessAzureASActivity");
                }
                catch (Exception ex)
                {
                    logger.Write(ex.Message);
                    throw;
                }
            }
            else
            {
                logger.Write("Custom TMSL script specified, perform action defined in TMSL script");
                try
                {
                    using (AdomdConnection asConn = new AdomdConnection(context.AzureASConnectionString))
                    {
                        asConn.Open();
                        foreach (string scriptPath in context.AdvancedASProcessingScriptPath.Split(';'))
                        {
                            string commandText = ReadBlob(context.BlobStorageConnectionString, scriptPath);
                            AdomdCommand asCmd = asConn.CreateCommand();
                            asCmd.CommandText = commandText;
                            asCmd.ExecuteNonQuery();
                            logger.Write("Azure AS was successfully processed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Write(ex.Message);
                    throw;
                }

            }
            
            return new Dictionary<string, string>();
        }

        internal virtual Model GetTabularModel(string aasConnectionString, string tabularDatabaseName)
        {
            var analysisServicesServer = new Server();
            analysisServicesServer.Connect(aasConnectionString);

            var tabularDatabase = analysisServicesServer.Databases.FindByName(tabularDatabaseName);

            if (tabularDatabase == null) {
                throw new ArgumentException("Database not found", tabularDatabaseName);
            }
            return tabularDatabase.Model;
        }

        internal virtual void ProcessTabularModel(Model tabularModel, IActivityLogger logger)
        {
            // We request a refresh for all tables
            foreach (var table in tabularModel.Tables)
            {
                // For partition tables, we process each partition.
                if (table.Partitions.Any())
                {
                    logger.Write("Table {0} will be processed partition by partition", table.Name);

                    foreach (var partition in table.Partitions)
                    {
                        partition.RequestRefresh(RefreshType.Full);
                    }
                }
                else
                {
                    logger.Write("Table {0} will be processed in full mode", table.Name);
                    table.RequestRefresh(RefreshType.Full);
                }
            }

            logger.Write("Azure AS processing started");

            tabularModel.SaveChanges();

            logger.Write("Azure AS was successfully processed");
        }

        private void ValidateParameters(IEnumerable<LinkedService> linkedServices, IEnumerable<Dataset> datasets,
                  Activity activity, IActivityLogger logger)
        {
            if (linkedServices == null) throw new ArgumentNullException("linkedServices");
            if (datasets == null) throw new ArgumentNullException("datasets");
            if (activity == null) throw new ArgumentNullException("activity");
            if (logger == null) throw new ArgumentNullException("logger");

            // Verify datasets
            if (activity.Outputs.Count != 1) throw new ArgumentException("Only one output datasets is required, as a dummy");

            foreach (LinkedService ls in linkedServices)
                logger.Write("Detected linkedService.Name {0}", ls.Name);

            DotNetActivity dotNetActivity = (DotNetActivity)activity.TypeProperties;

            // Ensure required parameters are included
            if (!dotNetActivity.ExtendedProperties.ContainsKey(ADV_AS_PROCESS_SCRIPT_PATH_PARAMETER_NAME)) {
                if (!dotNetActivity.ExtendedProperties.ContainsKey(TABULAR_DATABASE_NAME_PARAMETER_NAME)) throw new ArgumentException(TABULAR_DATABASE_NAME_PARAMETER_NAME);
            }
            if (!dotNetActivity.ExtendedProperties.ContainsKey(AZUREAS_CONNECTION_STRING_PARAMETER_NAME)) throw new ArgumentException(AZUREAS_CONNECTION_STRING_PARAMETER_NAME);

            logger.Write("Parameters validated");
        }

        private ProcessAzureASContext CreateContext(IEnumerable<LinkedService> linkedServices, IEnumerable<Dataset> datasets, Activity activity, IActivityLogger logger)
        {
            //Get Azure Storage Linked Service Connection String from the dummy output dataset,
            //AS processing does not produce output dataset, so we use this to access the TMSL script for AS processing
          
            AzureStorageLinkedService outputLinkedService;
            Dataset outputDataset = datasets.Single(dataset => dataset.Name == activity.Outputs.Single().Name);

            AzureBlobDataset outputTypeProperties;
            outputTypeProperties = outputDataset.Properties.TypeProperties as AzureBlobDataset;

            // get the  Azure Storate linked service from linkedServices object            
            outputLinkedService = linkedServices.First(
                linkedService =>
                linkedService.Name ==
                outputDataset.Properties.LinkedServiceName).Properties.TypeProperties
                as AzureStorageLinkedService;

            // get the connection string in the linked service
            string blobconnectionString = outputLinkedService.ConnectionString;

            DotNetActivity dotNetActivity = (DotNetActivity)activity.TypeProperties;

            var tabularDatabaseName = dotNetActivity.ExtendedProperties[TABULAR_DATABASE_NAME_PARAMETER_NAME];
            var aasConnectionString = dotNetActivity.ExtendedProperties[AZUREAS_CONNECTION_STRING_PARAMETER_NAME];
            var advASProcessingScriptPath="";
            if (dotNetActivity.ExtendedProperties.ContainsKey(ADV_AS_PROCESS_SCRIPT_PATH_PARAMETER_NAME))
            {
                advASProcessingScriptPath = dotNetActivity.ExtendedProperties[ADV_AS_PROCESS_SCRIPT_PATH_PARAMETER_NAME];
            }

            if (dotNetActivity.ExtendedProperties.ContainsKey(AZUREAD_AUTHORITY_PARAMETER_NAME))
            {
                aasConnectionString = GetAzureADToken(blobconnectionString, dotNetActivity, aasConnectionString);
            }

            return new ProcessAzureASContext
            {
                TabularDatabaseName = tabularDatabaseName,
                AzureASConnectionString = aasConnectionString,
                AdvancedASProcessingScriptPath= advASProcessingScriptPath,
                BlobStorageConnectionString= blobconnectionString
            };
        }

        private static string GetAzureADToken(string blobConnectionString, DotNetActivity dotNetActivity, string aasConnectionString)
        {
            string authority = dotNetActivity.ExtendedProperties[AZUREAD_AUTHORITY_PARAMETER_NAME];
            string resource = dotNetActivity.ExtendedProperties[AZUREAD_RESOURCE_PARAMETER_NAME];
            string clientId = dotNetActivity.ExtendedProperties[AZUREAD_CLIENTID_PARAMETER_NAME];
            string clientSecretPath = dotNetActivity.ExtendedProperties[AZUREAD_CLIENTSECRETPATH_PARAMETER_NAME];

            string clientSecret = ReadBlob(blobConnectionString, clientSecretPath);

            AuthenticationContext authContext = new AuthenticationContext(authority);
            ClientCredential cc = new ClientCredential(clientId, clientSecret);
            var task = authContext.AcquireTokenAsync(resource, cc);
            task.Wait();
            AuthenticationResult token = task.Result;
            aasConnectionString = string.Format(CultureInfo.InvariantCulture, aasConnectionString, token.AccessToken);
            return aasConnectionString;
        }

        private static string ReadBlob(string blobConnectionString, string blobPath)
        {
            string[] pathArr = blobPath.Split("/".ToCharArray(), 2);
            if (pathArr.Count() < 2)
            {
                throw new ArgumentException("Missing container name", ADV_AS_PROCESS_SCRIPT_PATH_PARAMETER_NAME);
            }
            string container = pathArr.First();
            string filepath = pathArr.Last();

            CloudStorageAccount inputStorageAccount = CloudStorageAccount.Parse(blobConnectionString);
            CloudBlobClient inputClient = inputStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer inputContainer = inputClient.GetContainerReference(container);
            CloudBlockBlob blockBlob = inputContainer.GetBlockBlobReference(filepath);

            string CmdStr;

            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);
                memoryStream.Position = 0;
                StreamReader CmdReader = new StreamReader(memoryStream);
                CmdStr = CmdReader.ReadToEnd();
            }
            return CmdStr;
        }
    }
}
