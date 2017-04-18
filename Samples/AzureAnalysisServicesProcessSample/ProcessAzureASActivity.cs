namespace AzureAnalysisServicesProcessSample
{
    using Microsoft.AnalysisServices.Tabular;
    using Microsoft.AnalysisServices
    using Microsoft.Azure.Management.DataFactories.Models;
    using Microsoft.Azure.Management.DataFactories.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    


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


        internal override ProcessAzureASContext PreExecute(IEnumerable<LinkedService> linkedServices, IEnumerable<Dataset> datasets, Activity activity, IActivityLogger logger)
        {
            ValidateParameters(linkedServices, datasets, activity, logger);

            return CreateContext(linkedServices, activity, logger);
        }

        public override IDictionary<string, string> Execute(ProcessAzureASContext context, IActivityLogger logger)
        {
            logger.Write("Starting ProcessAzureASActivity");

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

            return new Dictionary<string, string>();
        }

        internal virtual Model GetTabularModel(string aasConnectionString, string tabularDatabaseName)
        {
            var analysisServicesServer = new Server();
            analysisServicesServer.Connect(aasConnectionString);

            var tabularDatabase = analysisServicesServer.Databases.FindByName(tabularDatabaseName);

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
            if (!activity.Inputs.Any()) throw new ArgumentException("At least one input dataset is required");
            if (activity.Outputs.Count != 1) throw new ArgumentException("Only one output datasets is required, as a dummy");

            foreach (LinkedService ls in linkedServices)
                logger.Write("Detected linkedService.Name {0}", ls.Name);

            DotNetActivity dotNetActivity = (DotNetActivity)activity.TypeProperties;

            // Ensure required parameters are included
            if (!dotNetActivity.ExtendedProperties.ContainsKey(TABULAR_DATABASE_NAME_PARAMETER_NAME)) throw new ArgumentException(TABULAR_DATABASE_NAME_PARAMETER_NAME);
            if (!dotNetActivity.ExtendedProperties.ContainsKey(AZUREAS_CONNECTION_STRING_PARAMETER_NAME)) throw new ArgumentException(AZUREAS_CONNECTION_STRING_PARAMETER_NAME);

            logger.Write("Parameters validated");
        }

        private ProcessAzureASContext CreateContext(IEnumerable<LinkedService> linkedServices, Activity activity, IActivityLogger logger)
        {
            DotNetActivity dotNetActivity = (DotNetActivity)activity.TypeProperties;

            var tabularDatabaseName = dotNetActivity.ExtendedProperties[TABULAR_DATABASE_NAME_PARAMETER_NAME];
            var aasConnectionString = dotNetActivity.ExtendedProperties[AZUREAS_CONNECTION_STRING_PARAMETER_NAME];

            return new ProcessAzureASContext
            {
                TabularDatabaseName = tabularDatabaseName,
                AzureASConnectionString = aasConnectionString
            };
        }
    }
}
