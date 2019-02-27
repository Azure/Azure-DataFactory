using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    /// <summary>
    /// Builds, resolves tokens from KeyVault, and deploys the ADF project
    /// </summary>
    public class PublishManager
    {
        private ILogger logger;
        private IBlobUtilities blob;
        private IAdfFileHelper adfFileHelper;
        private IHttpClient httpClient;
        private SettingsContext settingsContext;

        public PublishManager(IKeyVaultResolver keyVault, SettingsContext settingsContext, ILogger logger = null, HttpClientProxy httpClient = null)
        {

            if (logger == null)
            {
                this.logger = new Logger();
            }
            else
            {
                this.logger = logger;
            }

            if (httpClient == null)
            {
                this.httpClient = new HttpClientProxy();
            }
            else
            {
                this.httpClient = httpClient;
            }


            blob = new BlobUtilities(logger);
            adfFileHelper = new AdfFileHelper(keyVault, logger);

            this.settingsContext = settingsContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishManager"/> class. This constructor is used for testing.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="keyVault">The key vault.</param>
        /// <param name="blob">The BLOB.</param>
        /// <param name="settingsContextManager">The settings context manager.</param>
        public PublishManager(ILogger logger, IKeyVaultResolver keyVault, IBlobUtilities blob, ISettingsContextManager settingsContextManager)
        {
            this.logger = logger;
            this.blob = blob;
            adfFileHelper = new AdfFileHelper(keyVault, logger);
        }

        /// <summary>
        /// Performs all steps required for the secure publish including building, resolving deployment settings, resolving key vault, and deploying files and custom activities to Azure.
        /// </summary>
        public async Task BuildAndSecurePublish(string projectPath, DataFactoryInfo dataFactory)
        {
            string dataFactoryName = dataFactory.Name;
            string dataFactoryResourceGroup = dataFactory.ResourceGroup;

            AdfBuild build = new AdfBuild(logger);

            Task<bool> buildTask = build.Build(projectPath);
            Task schemaDownloadTask = adfFileHelper.GetSchemas(httpClient);

            bool buildResult = await buildTask;
            await schemaDownloadTask;


            if (buildResult)
            {
                // Only debug builds are currently supported by the ADF build process
                string adfOutputFolder = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\debug");

                var publishResult = await PublishFromOutputFolder(adfOutputFolder, dataFactoryResourceGroup, dataFactoryName);

                if (publishResult)
                {
                    logger.Write(string.Empty);
                    logger.Write("Publish complete", "Black");
                    logger.Write(string.Empty);
                }
            }
            else
            {
                logger.Write("Build failed", "Red");
                logger.Write(string.Empty);
            }
        }

        /// <summary>
        /// Publishes to the specified data factory given a prebuilt output folder
        /// </summary>
        /// <returns>True if the publish succeeds othewise false</returns>
        public async Task<bool> PublishFromOutputFolder(string adfOutputFolder, string dataFactoryResourceGroup, string dataFactoryName)
        {
            try
            {
                // Get schemas used for determining ADF files
                await adfFileHelper.GetSchemas(httpClient);

                List<string> filesToProcess = Directory.GetFiles(adfOutputFolder, "*.json", SearchOption.TopDirectoryOnly).ToList();

                var lowerExcludeList = settingsContext.FilesToExclude == null || !settingsContext.FilesToExclude.Any()
                    ? new List<string>()
                    : settingsContext.FilesToExclude.Select(x => x.ToLowerInvariant());

                filesToProcess = filesToProcess.Where(x =>
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(x);
                    return fileNameWithoutExtension != null &&
                           !lowerExcludeList.Contains(fileNameWithoutExtension.ToLowerInvariant());
                }).ToList();

                string deployConfigPath = string.IsNullOrEmpty(settingsContext.DeploymentConfigName)
                    ? null
                    : filesToProcess.FirstOrDefault(x =>
                    {
                        var fileName = Path.GetFileName(x);
                        return fileName != null &&
                               fileName.Equals(Path.GetFileNameWithoutExtension(settingsContext.DeploymentConfigName) + ".json",
                                   StringComparison.InvariantCultureIgnoreCase);
                    });

                DeployConfigInfo deployConfig = null;

                logger.Write(string.Empty);

                if (!string.IsNullOrEmpty(deployConfigPath))
                {
                    logger.Write("Using deployment configuration file: " + deployConfigPath);
                    deployConfig = adfFileHelper.GetDeployConfigInfo(deployConfigPath);
                }
                else
                {
                    logger.Write("No deployment configuration file found.", "Orange");
                }

                AdfDeploy adf = new AdfDeploy(adfFileHelper, logger, blob, settingsContext, dataFactoryResourceGroup, dataFactoryName);
                
                return await adf.Deploy(filesToProcess, adfOutputFolder, deployConfig);
            }
            catch (Exception e)
            {
                logger.Write($"Error: {e.Message}");
                logger.WriteError(e);
                return false;
            }
        }
    }
}
