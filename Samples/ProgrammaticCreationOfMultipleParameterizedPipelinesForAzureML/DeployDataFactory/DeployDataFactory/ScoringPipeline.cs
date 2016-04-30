using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;
using System.Collections.ObjectModel;

using Microsoft.Azure.Management.DataFactories;
using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.Azure.Management.DataFactories.Common.Models;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure;

namespace DeployDataFactory
{
    class ScoringPipeline
    {
        public static void CreateObjects(string[] parameters, DataFactoryManagementClient client)
        {
            string inputTable;
            IList<string> outputTables;
            CreateInputOutputTables(DataFactoryConfig.ResourceGroupName, DataFactoryConfig.DataFactoryName, client, out inputTable, out outputTables, parameters);

            CreatePipelines(DataFactoryConfig.ResourceGroupName, DataFactoryConfig.DataFactoryName, client, inputTable, outputTables, parameters);
        }

        private static void CreatePipelines(string resourceGroupName, string dataFactoryName, DataFactoryManagementClient client, string inputTable, IList<string> outputTables, string[] parameters)
        {
            int i = 0;
            foreach (string parameter in parameters)
            {
                string[] parameterList = parameter.Split(',');
                string region = parameterList[0];

                // create a pipeline           
                DateTime PipelineActivePeriodStartTime = DateTime.Parse(DataFactoryConfig.PipelineStartTime);
                DateTime PipelineActivePeriodEndTime = PipelineActivePeriodStartTime.AddMinutes(DataFactoryConfig.MinutesToAddToStartTimeForEndTime);
                string PipelineName = String.Format("ScoringPipeline_{0}", region);
                Console.WriteLine("Creating a pipeline {0}", PipelineName);

                client.Pipelines.CreateOrUpdate(resourceGroupName, dataFactoryName,
                    new PipelineCreateOrUpdateParameters()
                    {
                        Pipeline = new Pipeline()
                        {
                            Name = PipelineName,
                            Properties = new PipelineProperties()
                            {
                                Description = "Pipeline for scoring",

                                // Initial value for pipeline's active period. With this, you won't need to set slice status
                                Start = PipelineActivePeriodStartTime,
                                End = PipelineActivePeriodEndTime,

                                Activities = new List<Activity>()
                                {                                
                                    new Activity()
                                    {   
                                        Name = "AzureMLBatchScoring",
                                        Inputs = new List<ActivityInput>()
                                        {
                                            new ActivityInput() {
                                                Name = inputTable
                                            }
                                        },
                                        Outputs = new List<ActivityOutput>()
                                        {
                                            new ActivityOutput()
                                            {
                                                Name = outputTables[i]
                                            }
                                        },
                                        LinkedServiceName = String.Format("{0}{1}",DataFactoryConfig.ScoringLinkedServiceNamePrefix,region),
                                        TypeProperties = new AzureMLBatchExecutionActivity()
                                        {
                                            WebServiceInput = inputTable,
                                            WebServiceOutputs = new Dictionary<string, string>
                                            {
                                              {"output1", outputTables[i]}                                                        
                                            },
                                            GlobalParameters = new Dictionary<string, string>
                                            {                                                
                                                {"starttime", "$$Text.Format('\\'{0:yyyy-MM-dd HH:mm:ss}\\'', WindowStart)"},
                                                {"endtime", "$$Text.Format('\\'{0:yyyy-MM-dd HH:mm:ss}\\'', WindowEnd)"},
                                                {"cpf", region}
                                            }                                                       
                                        }
                                    }
                                },
                            }
                        }
                    });
                i++;
            }
        }

        private static void CreateInputOutputTables(string resourceGroupName, string dataFactoryName, DataFactoryManagementClient client, out string inputTable, out IList<string> outputTables, string[] parameters)
        {
            inputTable = "InputTableScoring";
            outputTables = new List<string>();
            foreach (string parameter in parameters)
            {
                string[] parameterList = parameter.Split(',');
                string region = parameterList[0];
                string tableName = String.Format("outputScoring_{0}", region);

                client.Datasets.CreateOrUpdate(resourceGroupName, dataFactoryName,
                    new DatasetCreateOrUpdateParameters()
                    {
                        Dataset = new Dataset()
                        {
                            Name = tableName,
                            Properties = new DatasetProperties()
                            {

                                LinkedServiceName = "LinkedService-AzureStorage",
                                TypeProperties = new AzureBlobDataset()
                                {
                                    FolderPath = String.Format("outputscoring/{0}/", region) + "{Slice}",
                                    FileName = "output.csv",
                                    Format = new TextFormat()
                                    {
                                        ColumnDelimiter = ","
                                    },
                                    PartitionedBy = new Collection<Partition>()
                                    {
                                        new Partition()
                                        {
                                            Name = "Slice",
                                            Value = new DateTimePartitionValue()
                                            {
                                                Date = "SliceStart",
                                                Format = "yyyyMMdd-HHmmss"
                                            }
                                        }
                                    }
                                },

                                Availability = new Availability()
                                {
                                    Frequency = SchedulePeriod.Minute,
                                    Interval = DataFactoryConfig.PipelineFrequencyInMinutes,
                                },
                            }
                        }                        
                    });
                outputTables.Add(tableName);                           
            }
        }
    }
}
