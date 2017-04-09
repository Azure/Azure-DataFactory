using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ADF.DotNetActivityRunner;
using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.Azure.Management.DataFactories.Runtime;

namespace CustomActivityRunner
{
    public abstract class CustomActivityBase : IDotNetActivity
    {
        public IEnumerable<LinkedService> LinkedServices { get; private set; }

        public IEnumerable<Dataset> Datasets { get; private set; }

        public Activity Activity { get; private set; }

        public IActivityLogger Logger { get; private set; }

        private DotNetActivity typeProperties;

        public CustomActivityBase()
        {
            if (Debugger.IsAttached)
            {
                var attributes = this.GetType().GetMethod("RunActivity").CustomAttributes;
                var customActivityAttribute = attributes.FirstOrDefault(x => x.AttributeType.Name == "CustomActivityAttribute");

                string activityName = customActivityAttribute?.NamedArguments?.FirstOrDefault(x => x.MemberName == "ActivityName").TypedValue.Value?.ToString();
                string pipelineLocation = customActivityAttribute?.NamedArguments?.FirstOrDefault(x => x.MemberName == "PipelineLocation").TypedValue.Value?.ToString();
                string deployConfig = customActivityAttribute?.NamedArguments?.FirstOrDefault(x => x.MemberName == "DeployConfig").TypedValue.Value?.ToString();


                if (!string.IsNullOrEmpty(activityName) || !string.IsNullOrEmpty(pipelineLocation))
                {
                    string dataFactoryProjLocation =
                        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..\\..", Path.GetDirectoryName(pipelineLocation)));
                
                    DotNetActivityContext context = Runner.DeserializeActivity(Path.GetFileName(pipelineLocation), activityName, deployConfig, dataFactoryProjLocation);

                    LinkedServices = context.LinkedServices;
                    Datasets = context.Datasets;
                    Activity = context.Activity;
                    Logger = context.Logger;

                    typeProperties = Activity.TypeProperties as DotNetActivity;
                }
                else
                {
                    throw new Exception($"The CustomActivity attribute needs to have the following properties populated: {nameof(CustomActivityAttribute.PipelineLocation)} and {nameof(CustomActivityAttribute.ActivityName)}");
                }
            }
        }

        public IDictionary<string, string> Execute(IEnumerable<LinkedService> linkedServices, IEnumerable<Dataset> datasets, Activity activity, IActivityLogger logger)
        {
            LinkedServices = linkedServices;
            Datasets = datasets;
            Activity = activity;
            Logger = logger;

            typeProperties = Activity.TypeProperties as DotNetActivity;

            return RunActivity();
        }

        public abstract IDictionary<string, string> RunActivity();

        public string GetExtendedProperty(string name)
        {
            return typeProperties.ExtendedProperties[name];
        }

        public string GetInputSqlConnectionString()
        {
            string activityInputName = Activity.Inputs.First().Name;
            return GetSqlConnectionString(activityInputName);
        }
        public string GetOutputSqlConnectionString()
        {
            string activityOutputName = Activity.Outputs.First().Name;
            return GetSqlConnectionString(activityOutputName);
        }

        private string GetSqlConnectionString(string datasetName)
        {
            Dataset dataset = Datasets.Single(x => x.Name == datasetName);
            LinkedService linkedService = LinkedServices.First(x => x.Name == dataset.Properties.LinkedServiceName);

            if (linkedService.Properties.Type != "AzureSqlDatabase")
            {
                throw new Exception($"The linked service is of type '{linkedService.Properties.Type}'. It should be of type 'AzureSqlDatabase'.");
            }

            AzureSqlDatabaseLinkedService sqlLinkedService = linkedService.Properties.TypeProperties as AzureSqlDatabaseLinkedService;

            if (sqlLinkedService == null)
            {
                throw new Exception($"Unable to find data set name '{datasetName}'.");
            }

            string connectionString = sqlLinkedService.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"Connection string for '{linkedService.Name}' linked service is empty.");
            }

            return connectionString;
        }
    }
}
