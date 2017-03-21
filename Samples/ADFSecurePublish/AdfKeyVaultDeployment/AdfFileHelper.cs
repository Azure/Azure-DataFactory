using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public enum FileType
    {
        LinkedService,
        Table,
        Pipeline,
        Unknown
    }

    /// <summary>
    /// This class finds information on the ADF file types. It uses the ADF json schemas in order to determine which files they are.
    /// It also performs resolutions of deployment settings if specified in a deployment config and Key Vault tokens from the chosen Key Vault
    /// </summary>
    /// <seealso cref="Microsoft.ADF.Deployment.AdfKeyVaultDeployment.IAdfFileHelper" />
    public class AdfFileHelper : IAdfFileHelper
    {
        const string linkedServiceSchema = "http://datafactories.schema.management.azure.com/schemas/2015-09-01/Microsoft.DataFactory.LinkedService.json";
        const string tableSchema = "http://datafactories.schema.management.azure.com/schemas/2015-09-01/Microsoft.DataFactory.Table.json";
        const string configSchema = "http://datafactories.schema.management.azure.com/vsschemas/V1/Microsoft.DataFactory.Config.json";

        private JSchema jsonLinkedServiceSchema;
        private JSchema jsonTableSchema;
        private JSchema jsonPipelineSchema;
        private JSchema jsonConfigSchema;
        private IKeyVaultResolver keyVaultResolver;
        private ILogger logger;

        public AdfFileHelper(IKeyVaultResolver keyVaultResolver, ILogger logger)
        {
            this.keyVaultResolver = keyVaultResolver;
            this.logger = logger;
        }

        public async Task GetSchemas(IHttpClient httpClient)
        {
            // If publishing a second time, no need to get the schemas again
            if (jsonLinkedServiceSchema == null)
            {
                var tasks = new List<Task>
                {
                    httpClient.GetAsync(linkedServiceSchema).ContinueWith(x =>
                    {
                        jsonLinkedServiceSchema = JSchema.Parse(x.Result.Content.ReadAsStringAsync().Result,
                            new JSchemaUrlResolver());
                    }),
                    httpClient.GetAsync(configSchema).ContinueWith(x =>
                    {
                        jsonConfigSchema = JSchema.Parse(x.Result.Content.ReadAsStringAsync().Result,
                            new JSchemaUrlResolver());
                    })
                };

                await Task.WhenAll(tasks);

                // Get schema for pipelines
                var assembly = Assembly.GetExecutingAssembly();
                var pipelineSchemaResource = "Microsoft.ADF.Deployment.AdfKeyVaultDeployment.EmbeddedResources.PipelineSchema.json";
                var tableSchemaResource = "Microsoft.ADF.Deployment.AdfKeyVaultDeployment.EmbeddedResources.TableSchema.json";

                using (Stream stream = assembly.GetManifestResourceStream(pipelineSchemaResource))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string schemaText = reader.ReadToEnd();
                        jsonPipelineSchema = JSchema.Parse(schemaText, new JSchemaUrlResolver());
                    }
                }

                using (Stream stream = assembly.GetManifestResourceStream(tableSchemaResource))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string schemaText = reader.ReadToEnd();
                        jsonTableSchema = JSchema.Parse(schemaText, new JSchemaUrlResolver());
                    }
                }

                httpClient.Dispose();
            }
        }

        /// <summary>
        /// Gets the information on the deployment configuration file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public DeployConfigInfo GetDeployConfigInfo(string filePath)
        {
            try
            {
                string fileContents = File.ReadAllText(filePath);
                JObject jObject = JObject.Parse(fileContents);

                if (jObject.IsValid(jsonConfigSchema))
                {
                    var deploymentDict = jObject.Properties()
                        .ToDictionary(x => x.Name,
                            y => y.Value.ToDictionary(z => z["name"].ToString(), z => z["value"].ToString()));

                    return new DeployConfigInfo
                    {
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        DeploymentDictionary = deploymentDict
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the information on the ADF file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="deployConfig">The deploy configuration.</param>
        public async Task<AdfFileInfo> GetFileInfo(string filePath, DeployConfigInfo deployConfig = null)
        {
            string fileContents = File.ReadAllText(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // Initialize props to default values
            AdfFileInfo fileInfo = new AdfFileInfo
            {
                FileType = FileType.Unknown,
                SubType = string.Empty,
                Name = string.Empty,
                IsValid = false,
                FileContents = fileContents,
                FileName = fileName
            };

            JObject jObject = null;

            try
            {
                jObject = JObject.Parse(fileContents);

                JObject propertyJObject = jObject.GetValue("properties", StringComparison.OrdinalIgnoreCase) as JObject;
                JToken nameJToken = jObject.GetValue("name", StringComparison.OrdinalIgnoreCase);

                if (propertyJObject == null || nameJToken == null)
                {
                    logger.Write($"{fileInfo.FileName} is a not a valid ADF file.");
                    return fileInfo;
                }

                fileInfo.Name = nameJToken.ToObject<string>();

                if (deployConfig != null)
                {
                    // Update ADF files with deploymnet settings if they exist
                    jObject = ResolveDeploymentSettings(jObject, fileInfo.Name, deployConfig);
                    fileInfo.FileContents = jObject.ToString();
                }

                JToken typeJToken = propertyJObject.GetValue("type", StringComparison.OrdinalIgnoreCase);
                if (typeJToken != null)
                {
                    fileInfo.SubType = typeJToken.ToObject<string>();
                }

                if (fileInfo.SubType == "CPSServiceBusProxy" || jObject.IsValid(jsonLinkedServiceSchema))
                {
                    fileInfo.FileType = FileType.LinkedService;
                    fileInfo.IsValid = true;

                    logger.Write($"Retreived LinkedService: {fileInfo.FileName}");
                }
                else if (jObject.IsValid(jsonTableSchema))
                {
                    fileInfo.FileType = FileType.Table;
                    fileInfo.IsValid = true;

                    logger.Write($"Retreived Dataset: {fileInfo.FileName}");
                }
                else if (jObject.IsValid(jsonPipelineSchema))
                {
                    fileInfo.FileType = FileType.Pipeline;
                    fileInfo.IsValid = true;

                    logger.Write($"Retreived Pipeline: {fileInfo.FileName}");

                    // Get all custom activity packages if available
                    JArray activities = propertyJObject.GetValue("activities", StringComparison.InvariantCultureIgnoreCase) as JArray;

                    if (activities != null)
                    {
                        fileInfo.CustomActivityPackages = new List<CustomActivityPackageInfo>();

                        foreach (JObject activity in activities)
                        {
                            JToken activityTypeJToken = activity.GetValue("type", StringComparison.OrdinalIgnoreCase);

                            if (activityTypeJToken != null && activityTypeJToken.ToObject<string>().Equals("DotNetActivity", StringComparison.CurrentCultureIgnoreCase))
                            {
                                CustomActivityPackageInfo packageInfo = new CustomActivityPackageInfo();

                                packageInfo.PackageLinkedService = activity.SelectToken("$.typeProperties.packageLinkedService")?.ToObject<string>();
                                packageInfo.PackageFile = activity.SelectToken("$.typeProperties.packageFile")?.ToObject<string>();

                                logger.Write($"Retreived Custom Activity package: {packageInfo.PackageFile}");

                                fileInfo.CustomActivityPackages.Add(packageInfo);
                            }
                        }
                    }
                }
                else
                {
                    fileInfo.FileType = FileType.Unknown;
                    logger.Write($"{fileInfo.FileName} is a not a valid ADF file.");
                }
            }
            catch (Exception e)
            {
                fileInfo.ErrorException = e;
                logger.Write($"{fileInfo.FileName} is a not a valid ADF file. Error message: {e}");
                logger.WriteError(e);
            }

            if (fileInfo.IsValid)
            {
                // Search for keyvault tokens and resolve them
                string keyVaultResolvedContents = await ResolveKeyVault(fileInfo.FileContents);

                if (fileInfo.FileContents != keyVaultResolvedContents)
                {
                    fileInfo.FileContents = keyVaultResolvedContents;
                    fileInfo.JObject = JObject.Parse(fileInfo.FileContents);
                }
                else
                {
                    fileInfo.JObject = jObject;
                }
            }

            return fileInfo;
        }

        /// <summary>
        /// Resolves the deployment settings from the deployment config.
        /// </summary>
        private JObject ResolveDeploymentSettings(JObject jObject, string name, DeployConfigInfo deployConfig)
        {
            // Update with values from delpoyment config if exists
            if (deployConfig.DeploymentDictionary.Count > 0 && deployConfig.DeploymentDictionary.ContainsKey(name))
            {
                foreach (KeyValuePair<string, string> pair in deployConfig.DeploymentDictionary[name])
                {
                    JToken token = jObject.SelectToken(pair.Key);

                    if (token == null)
                    {
                        logger.Write(
                            $"The deployment configuration file '{deployConfig.FileName}' contains a substitution for '{name}' however the JPath '{pair.Key}' is not found in '{name}'.");
                    }
                    else
                    {
                        token.Replace(pair.Value);
                    }
                }
            }

            return jObject;
        }

        /// <summary>
        /// Resolves the key vault tokens with the actual values from Key Vault.
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        public async Task<string> ResolveKeyVault(string fileContents)
        {
            var matches = Regex.Matches(fileContents, "\\<KeyVault:(?<key>.*?)\\>");

            foreach (Match match in matches)
            {
                string value = match.Groups["key"].Value.Trim();

                logger.Write($"Found Key Vault token '{value}'. Resolving from Key Vault '{keyVaultResolver.KeyVaultName}'.");

                // Get keyvault value
                var secretTask = await keyVaultResolver.GetSecret(value);
                string secret = secretTask.Value;

                fileContents = fileContents.Replace($"<KeyVault:{value}>", secret);
            }

            return fileContents;
        }
    }
}
