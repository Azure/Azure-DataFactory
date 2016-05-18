// Copyright (c) Microsoft Corporation. All Rights Reserved.

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
        public static void CreateObjects(
            string[] parameters, 
            DataFactoryManagementClient client)
        {
            string inputDataset;
            IList<string> outputDatasets;
            CreateInputOutputDatasets(
                DataFactoryConfig.ResourceGroupName, 
                DataFactoryConfig.DataFactoryName, 
                client, out inputDataset, 
                out outputDatasets, 
                parameters);

            CreatePipelines(
                DataFactoryConfig.ResourceGroupName, 
                DataFactoryConfig.DataFactoryName, 
                client, inputDataset, 
                outputDatasets, 
                parameters);
        }

        private static void CreatePipelines(
            string resourceGroupName, 
            string dataFactoryName, 
            DataFactoryManagementClient client, 
            string inputDataset, 
            IList<string> outputDatasets, 
            string[] parameters)
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

                                // Initial value for pipeline's active period.
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
                                                Name = inputDataset
                                            }
                                        },
                                        Outputs = new List<ActivityOutput>()
                                        {
                                            new ActivityOutput()
                                            {
                                                Name = outputDatasets[i]
                                            }
                                        },
                                        // Note:  The linked service names referenced here are generated previously by the retraining pipeline code.                                       
                                        LinkedServiceName = Utilities.GetScoringLinkedServiceName(DataFactoryConfig.ScoringLinkedServiceNamePrefix, region),
                                        TypeProperties = new AzureMLBatchExecutionActivity()
                                        {
                                            WebServiceInput = inputDataset,
                                            WebServiceOutputs = new Dictionary<string, string>
                                            {
                                              {"output1", outputDatasets[i]}                                                        
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

        private static void CreateInputOutputDatasets(
            string resourceGroupName, 
            string dataFactoryName, 
            DataFactoryManagementClient client, 
            out string inputDataset, 
            out IList<string> outputDatasets, 
            string[] parameters)
        {
            inputDataset = "InputDatasetScoring";
            outputDatasets = new List<string>();
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
                outputDatasets.Add(tableName);                           
            }
        }
    }
}
