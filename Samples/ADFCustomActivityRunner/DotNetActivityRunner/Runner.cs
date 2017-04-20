using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;
using Microsoft.Azure.Management.DataFactories.Common.Models;
using Microsoft.Azure.Management.DataFactories.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.ADF.DotNetActivityRunner
{
    public class Runner
    {
        public static DotNetActivityContext DeserializeActivity(string pipelineFileName, string activityName, string configFile = null, string adfFilesPath = @"..\..\..\McioppDataFactory")
        {
            // Get Key Vault settings if secure publish is being used on the local machine
            AdfFileHelper adfFileHelper = null;
            string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SecurePublishSettings.json");
            if (File.Exists(settingsFile))
            {
                AppSettings settings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(settingsFile));
                X509Certificate2 cert = KeyVaultResolver.FindCertificateByThumbprint(settings.KeyVaultCertThumbprint);
                string suffix = settings.EnvironmentSettings.First().KeyVaultDnsSuffix;
                suffix = string.IsNullOrEmpty(suffix) ? "vault.azure.net:443" : suffix;
                KeyVaultResolver keyVaultResolver = new KeyVaultResolver(settings.EnvironmentSettings.First().KeyVaultName, suffix, settings.KeyVaultCertClientId, cert);
                adfFileHelper = new AdfFileHelper(keyVaultResolver, new Logger());
            }

            adfFilesPath = Path.GetFullPath(adfFilesPath);

            var deploymentDict = new Dictionary<string, Dictionary<string, string>>();
            if (!string.IsNullOrEmpty(configFile))
            {
                // Get deployment config
                string deploymentConfigPath = Path.Combine(adfFilesPath, configFile);
                var deploymentConfigJson = File.ReadAllText(deploymentConfigPath);
                var deploymentJObj = JObject.Parse(deploymentConfigJson);
                deploymentDict = deploymentJObj.Properties()
                    .ToDictionary(x => x.Name,
                        y => y.Value.ToDictionary(z => z["name"].ToString(), z => z["value"].ToString()));
            }

            DotNetActivityContext context = new DotNetActivityContext
            {
                LinkedServices = new List<LinkedService>(),
                Datasets = new List<Dataset>(),
                Activity = new Activity(),
                Logger = new ActivityLogger()
            };

            string pipelinePath = Path.Combine(adfFilesPath, pipelineFileName);
            string pipelineJson = File.ReadAllText(pipelinePath);
            
            string pipelineName = Path.GetFileNameWithoutExtension(pipelineFileName);

            // Update with values from delpoyment config if exists
            if (deploymentDict.Count > 0 && deploymentDict.ContainsKey(pipelineName))
            {
                JObject pipelineJObject = JObject.Parse(pipelineJson);

                foreach (KeyValuePair<string, string> pair in deploymentDict[pipelineName])
                {
                    JToken token = pipelineJObject.SelectToken(pair.Key);
                    token.Replace(pair.Value);
                }

                pipelineJson = pipelineJObject.ToString();
            }

            // Search for Key Vault references in the pipeline and replace with their Key Vault equivalents if found
            if (adfFileHelper != null)
            {
                pipelineJson = adfFileHelper.ResolveKeyVault(pipelineJson).Result;
            }

            var dummyPipeline = JsonConvert.DeserializeObject<Models.Pipeline>(pipelineJson);

            Models.Activity dummyActivity;
            try
            {
                dummyActivity = dummyPipeline.Properties.Activities.Single(x => x.Name == activityName);
            }
            catch (InvalidOperationException)
            {
                throw new Exception($"Activity {activityName} not found in {pipelinePath}.");
            }

            context.Activity.Name = dummyActivity.Name;

            context.Activity.TypeProperties = new DotNetActivity();
            DotNetActivity dotNetActivity = (DotNetActivity)context.Activity.TypeProperties;
            dotNetActivity.ExtendedProperties = dummyActivity.DotNetActivityTypeProperties.ExtendedProperties;

            // get the input and output tables
            var dummyDatasets = new HashSet<Models.ActivityData>();
            dummyDatasets.UnionWith(dummyActivity.Inputs);
            dummyDatasets.UnionWith(dummyActivity.Outputs);

            var dummyServices = new HashSet<Models.LinkedService>();

            // init the data tables
            foreach (var dummyDataset in dummyDatasets)
            {
                // parse the table json source
                var dataPath = Path.Combine(adfFilesPath, dummyDataset.Name + ".json");
                var dataJson = File.ReadAllText(dataPath);
                var dummyTable = JsonConvert.DeserializeObject<Models.Table>(dataJson);
                {
                    // initialize dataset properties
                    DatasetTypeProperties datasetProperties;
                    switch (dummyTable.Properties.Type)
                    {
                        case "AzureBlob":
                            // init the azure model
                            var blobDataset = new AzureBlobDataset
                            {
                                FolderPath = dummyTable.Properties.TypeProperties.FolderPath,
                                FileName = dummyTable.Properties.TypeProperties.FileName
                            };

                            datasetProperties = blobDataset;
                            break;

                        case "AzureTable":
                        case "AzureSqlTable":
                            var tableDataset = new AzureTableDataset
                            {
                                TableName = dummyTable.Properties.TypeProperties.TableName
                            };

                            datasetProperties = tableDataset;
                            break;

                        case "SqlServerTable":
                            var sqlTableDataset = new SqlServerTableDataset(dummyTable.Properties.TypeProperties.TableName);

                            datasetProperties = sqlTableDataset;
                            break;

                        default:
                            throw new Exception($"Unexpected Dataset.Type {dummyTable.Properties.Type}");
                    }

                    // initialize dataset

                    var dataDataset = new Dataset(
                        dummyDataset.Name,
                        new DatasetProperties(
                            datasetProperties,
                            new Availability(),
                            string.Empty
                        )
                    );

                    dataDataset.Properties.LinkedServiceName = dummyTable.Properties.LinkedServiceName;
                    context.Datasets.Add(dataDataset);

                }

                // register the inputs and outputs in the activity
                if (dummyDataset is Models.ActivityInput)
                {
                    context.Activity.Inputs.Add(new ActivityInput(dummyDataset.Name));
                }

                if (dummyDataset is Models.ActivityOutput)
                {
                    context.Activity.Outputs.Add(new ActivityOutput(dummyDataset.Name));
                }

                // parse the linked service json source for later use
                string linkedServiceName = dummyTable.Properties.LinkedServiceName;
                var servicePath = Path.Combine(adfFilesPath, linkedServiceName + ".json");
                string serviceJson = File.ReadAllText(servicePath);
                
                string linkedServiceType = string.Empty;

                // Update with values from delpoyment config if exists
                if (deploymentDict.Count > 0 && deploymentDict.ContainsKey(linkedServiceName))
                {
                    JObject serviceJObject = JObject.Parse(serviceJson);
                    linkedServiceType = serviceJObject["properties"]["type"].ToObject<string>();

                    foreach (KeyValuePair<string, string> pair in deploymentDict[linkedServiceName])
                    {
                        JToken token = serviceJObject.SelectToken(pair.Key);
                        token.Replace(pair.Value);
                    }

                    serviceJson = serviceJObject.ToString();
                }
                else
                {
                    JObject serviceJObject = JObject.Parse(serviceJson);
                    linkedServiceType = serviceJObject["properties"]["type"].ToObject<string>();
                }

                // Search for Key Vault references in the linked service and replace with their Key Vault equivalents if found
                if (adfFileHelper != null)
                {
                    serviceJson = adfFileHelper.ResolveKeyVault(serviceJson).Result;
                }

                Models.LinkedService storageService;

                switch (linkedServiceType)
                {
                    case "AzureSqlDatabase":
                    case "OnPremisesSqlServer":
                        storageService = JsonConvert.DeserializeObject<Models.AzureSqlDatabaseLinkedService>(serviceJson);
                        break;

                    case "AzureStorage":
                        storageService = JsonConvert.DeserializeObject<Models.StorageService>(serviceJson);
                        break;

                    default:
                        throw new Exception($"Mapper for linked service type '{linkedServiceType}' not found.");
                }

                dummyServices.Add(storageService);
            }

            // parse the hd insight service json source
            var computeServicePath = Path.Combine(adfFilesPath, dummyActivity.LinkedServiceName + ".json");
            var computeServiceJson = File.ReadAllText(computeServicePath);
            var computeService = JsonConvert.DeserializeObject<Models.ComputeService>(computeServiceJson);

            dummyServices.Add(computeService);


            // init the services
            foreach (var dummyService in dummyServices)
            {
                LinkedService linkedService = null;

                // init if it is a storage service
                if (dummyService is Models.StorageService)
                {
                    var dummyStorageService = dummyService as Models.StorageService;

                    var service = new AzureStorageLinkedService
                    {
                        ConnectionString = dummyStorageService.Properties.TypeProperties.ConnectionString
                    };

                    linkedService = new LinkedService(
                        dummyService.Name,
                        new LinkedServiceProperties(service)
                    );
                }

                // init if it is a AzureSqlDatabase service
                if (dummyService is Models.AzureSqlDatabaseLinkedService)
                {
                    var dummyStorageService = dummyService as Models.AzureSqlDatabaseLinkedService;

                    var service = new AzureSqlDatabaseLinkedService()
                    {
                        ConnectionString = dummyStorageService.Properties.TypeProperties.ConnectionString
                    };

                    linkedService = new LinkedService(
                        dummyService.Name,
                        new LinkedServiceProperties(service)
                    );
                }

                // init if it is a hd insight service
                if (dummyService is Models.ComputeService)
                {
                    var service = new HDInsightLinkedService();
                    linkedService = new LinkedService(
                        dummyService.Name,
                        new LinkedServiceProperties(service)
                    );
                }

                context.LinkedServices.Add(linkedService);
            }

            return context;
        }
    }
}
