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
    class RetrainingPipeline
    {
        public static void CreateObjects(
            string[] parameters, 
            DataFactoryManagementClient client, 
            IList<UpdateResourceEndpoint> endpoints)
        {
            CreateLinkedService(
                DataFactoryConfig.ResourceGroupName, 
                DataFactoryConfig.DataFactoryName, 
                client, endpoints, 
                parameters);

            string inputDataset;
            IList<string> outputModelDatasets;
            IList<string> outputPlaceholderDatasets;
            CreateInputOutputDatasets(
                DataFactoryConfig.ResourceGroupName, 
                DataFactoryConfig.DataFactoryName, 
                client, out inputDataset, 
                out outputModelDatasets, 
                out outputPlaceholderDatasets, 
                parameters, endpoints);

            CreatePipelines(
                DataFactoryConfig.ResourceGroupName, 
                DataFactoryConfig.DataFactoryName, 
                client, inputDataset, 
                outputModelDatasets, 
                outputPlaceholderDatasets, 
                parameters, 
                endpoints);
        }

        private static void CreatePipelines(
            string resourceGroupName, 
            string dataFactoryName, 
            DataFactoryManagementClient client, 
            string inputDataset, 
            IList<string> outputModelDatasets, 
            IList<string> outputPlaceholderDatasets, 
            string[] parameters, 
            IList<UpdateResourceEndpoint> endpoints)
        {
            int i = 0;
            foreach (string parameter in parameters)
            {
                string[] parameterList = parameter.Split(',');
                string region = parameterList[0];

                // create a pipeline           
                DateTime PipelineActivePeriodStartTime = DateTime.Parse(DataFactoryConfig.PipelineStartTime);
                DateTime PipelineActivePeriodEndTime = PipelineActivePeriodStartTime.AddMinutes(DataFactoryConfig.MinutesToAddToStartTimeForEndTime);
                string PipelineName = String.Format("RetrainingPipeline_{0}", region);
                Console.WriteLine("Creating a pipeline {0}", PipelineName);

                client.Pipelines.CreateOrUpdate(resourceGroupName, dataFactoryName,
                    new PipelineCreateOrUpdateParameters()
                    {
                        Pipeline = new Pipeline()
                        {
                            Name = PipelineName,
                            Properties = new PipelineProperties()
                            {
                                Description = "Pipeline for retraining",

                                // Initial value for pipeline's active period.
                                Start = PipelineActivePeriodStartTime,
                                End = PipelineActivePeriodEndTime,

                                Activities = new List<Activity>()
                                {                                
                                    new Activity()
                                    {   
                                        Name = "AzureMLBatchExecution",
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
                                                Name = outputModelDatasets[i]
                                            }
                                        },
                                        LinkedServiceName = "LinkedServiceRetraining-AzureML",
                                        TypeProperties = new AzureMLBatchExecutionActivity()
                                        {
                                            WebServiceInput = inputDataset,
                                            WebServiceOutputs = new Dictionary<string, string>
                                            {
                                              {"output1", outputModelDatasets[i]}                                                        
                                            },
                                            GlobalParameters = new Dictionary<string, string>
                                            {                                                
                                                {"starttime", "$$Text.Format('\\'{0:yyyy-MM-dd HH:mm:ss}\\'', WindowStart)"},
                                                {"endtime", "$$Text.Format('\\'{0:yyyy-MM-dd HH:mm:ss}\\'', WindowEnd)"},
                                                {"cpf", region}
                                            }                                                       
                                        }
                                    },
                                    new Activity()
                                    {   
                                        Name = "AzureMLUpdateResource",
                                        Inputs = new List<ActivityInput>()
                                        {
                                            new ActivityInput() {
                                                Name = outputModelDatasets[i]
                                            }
                                        },
                                        Outputs = new List<ActivityOutput>()
                                        {
                                            new ActivityOutput()
                                            {
                                                Name = outputPlaceholderDatasets[i]
                                            }
                                        },
                                        LinkedServiceName = "LinkedServiceScoring-AzureML-" + region,
                                        TypeProperties = new AzureMLUpdateResourceActivity()
                                        {
                                            TrainedModelName = "Trained model for facility " + region,
                                            TrainedModelDatasetName = outputModelDatasets[i]
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
            out IList<string> outputModelDatasets, 
            out IList<string> outputPlaceholderDatasets, 
            string[] parameters, 
            IList<UpdateResourceEndpoint> endpoints)
        {
            // create input and output tables
            Console.WriteLine("Creating input and output tables");
            inputDataset = "InputDatasetScoring";
            outputModelDatasets = new List<string>();
            outputPlaceholderDatasets = new List<string>();

            client.Datasets.CreateOrUpdate(resourceGroupName, dataFactoryName,
                new DatasetCreateOrUpdateParameters()
                {
                    Dataset = new Dataset()
                    {
                        Name = inputDataset,
                        Properties = new DatasetProperties()
                        {
                            LinkedServiceName = "LinkedService-AzureStorage",
                            TypeProperties = new AzureBlobDataset()
                            {
                                FolderPath = "inputdatascoring/",
                                FileName = "input.csv",
                                Format = new TextFormat()
                                {
                                    ColumnDelimiter = ","
                                }
                            },
                            External = true,
                            Availability = new Availability()
                            {
                                Frequency = SchedulePeriod.Minute,
                                Interval = 15,
                            },

                            Policy = new Policy()
                            {
                                Validation = new ValidationPolicy()
                                {
                                    MinimumRows = 1
                                }
                            }
                        }
                    }
                });

            foreach (string parameter in parameters)
            {
                string[] parameterList = parameter.Split(',');
                string region = parameterList[0];
                
                string outputModelDataset = String.Format("outputModel_{0}", region);

                client.Datasets.CreateOrUpdate(resourceGroupName, dataFactoryName,
                    new DatasetCreateOrUpdateParameters()
                    {
                        Dataset = new Dataset()
                        {
                            Name = outputModelDataset,
                            Properties = new DatasetProperties()
                            {
                                LinkedServiceName = "LinkedService-AzureStorage",
                                TypeProperties = new AzureBlobDataset()
                                {
                                    FolderPath = String.Format("outputmodel/{0}/", region) + "{Slice}",
                                    FileName = "model.ilearner",
                                    Format = new TextFormat()
                                    {                                        
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

                outputModelDatasets.Add(outputModelDataset);

                string outputPlaceholderDataset = String.Format("outputplaceholder_{0}", region);
                client.Datasets.CreateOrUpdate(resourceGroupName, dataFactoryName,
                    new DatasetCreateOrUpdateParameters()
                    {
                        Dataset = new Dataset()
                        {
                            Name = outputPlaceholderDataset,
                            Properties = new DatasetProperties()
                            {
                                LinkedServiceName = "LinkedService-AzureStorage",
                                TypeProperties = new AzureBlobDataset()
                                {
                                    FolderPath = "any",                                   
                                    Format = new TextFormat()
                                    {
                                    },
                                },

                                Availability = new Availability()
                                {
                                    Frequency = SchedulePeriod.Minute,
                                    Interval = 15,
                                },
                            }
                        }
                    });

                outputPlaceholderDatasets.Add(outputPlaceholderDataset);
            }
        }

        private static void CreateLinkedService(
            string resourceGroupName, 
            string dataFactoryName, 
            DataFactoryManagementClient client, 
            IList<UpdateResourceEndpoint> endpoints, 
            string[] parameters)
        {
            // create Azure ML training linked services
            Console.WriteLine("Creating Azure ML training linked service");
            client.LinkedServices.CreateOrUpdate(resourceGroupName, dataFactoryName,
                new LinkedServiceCreateOrUpdateParameters()
                {
                    LinkedService = new LinkedService()
                    {
                        Name = "LinkedServiceRetraining-AzureML",
                        Properties = new LinkedServiceProperties
                        (
                            new AzureMLLinkedService(DataFactoryConfig.RetrainingEndPoint, DataFactoryConfig.RetrainingApiKey )
                            {                                
                            }
                        )
                    }
                }
            );

            int i = 0;
            foreach (UpdateResourceEndpoint endpoint in endpoints)
            {
                string[] parameterList = parameters[i].Split(',');
                string region = parameterList[0];

                // create Azure ML scoring linked services
                Console.WriteLine("Creating Azure ML scoring linked service for {0}", endpoint);
                client.LinkedServices.CreateOrUpdate(resourceGroupName, dataFactoryName,
                    new LinkedServiceCreateOrUpdateParameters()
                    {
                        LinkedService = new LinkedService()
                        {
                            // Note: The linked service names generated here are also used by the scoring pipeline. 
                            Name = Utilities.GetScoringLinkedServiceName(DataFactoryConfig.ScoringLinkedServiceNamePrefix, region),
                            Properties = new LinkedServiceProperties
                            (
                                new AzureMLLinkedService(endpoint.mlEndpoint, endpoint.apiKey) 
                                {
                                    UpdateResourceEndpoint = endpoint.updateResourceEndpointUrl
                                }
                            )
                        }
                    }
                );
                i++;
            }
        }
    }    
}
