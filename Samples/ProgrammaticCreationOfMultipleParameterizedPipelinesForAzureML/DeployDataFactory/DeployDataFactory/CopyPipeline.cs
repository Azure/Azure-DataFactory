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
    class CopyPipeline
    {
        public static void CreateObjects(
                string[] parameters, 
                DataFactoryManagementClient client)
        {
            string Dataset_Source;
            string Dataset_Destination;
            CreateInputOutputDatasets(
                DataFactoryConfig.ResourceGroupName, 
                DataFactoryConfig.DataFactoryName, 
                client, out Dataset_Source, 
                out Dataset_Destination);

            CreatePipelines(
                DataFactoryConfig.ResourceGroupName, 
                DataFactoryConfig.DataFactoryName, 
                client, Dataset_Source, 
                Dataset_Destination, parameters);
        }

        private static void CreatePipelines
            (string resourceGroupName, 
            string dataFactoryName, 
            DataFactoryManagementClient client, 
            string Dataset_Source, 
            string Dataset_Destination, 
            string[] parameters)
        {
            foreach (string parameter in parameters)
            {
                string[] parameterList = parameter.Split(',');
                string region = parameterList[0];

                // create a pipeline           
                DateTime PipelineActivePeriodStartTime = DateTime.Parse(DataFactoryConfig.PipelineStartTime);
                DateTime PipelineActivePeriodEndTime = PipelineActivePeriodStartTime.AddMinutes(DataFactoryConfig.MinutesToAddToStartTimeForEndTime);
                string PipelineName = String.Format("Sample_{0}", region);
                Console.WriteLine("Creating a pipeline {0}", PipelineName);

                client.Pipelines.CreateOrUpdate(resourceGroupName, dataFactoryName,
                    new PipelineCreateOrUpdateParameters()
                    {
                        Pipeline = new Pipeline()
                        {
                            Name = PipelineName,
                            Properties = new PipelineProperties()
                            {
                                Description = "Demo Pipeline for data transfer between blobs",

                                // Initial value for pipeline's active period.
                                Start = PipelineActivePeriodStartTime,
                                End = PipelineActivePeriodEndTime,

                                Activities = new List<Activity>()
                                {                                
                                    new Activity()
                                    {   
                                        Name = "BlobToBlob",
                                        Inputs = new List<ActivityInput>()
                                        {
                                            new ActivityInput() {
                                                Name = Dataset_Source
                                            }
                                        },
                                        Outputs = new List<ActivityOutput>()
                                        {
                                            new ActivityOutput()
                                            {
                                                Name = Dataset_Destination
                                            }
                                        },
                                        TypeProperties = new CopyActivity()
                                        {
                                            Source = new BlobSource(),
                                            Sink = new BlobSink()
                                            {
                                                WriteBatchSize = 10000,
                                                WriteBatchTimeout = TimeSpan.FromMinutes(10)
                                            }
                                        }
                                    }

                                },
                            }
                        }
                    });
            }
        }

        private static void CreateInputOutputDatasets(
            string resourceGroupName, 
            string dataFactoryName, 
            DataFactoryManagementClient client, 
            out string Dataset_Source, 
            out string Dataset_Destination)
        {
            // create input and output tables
            Console.WriteLine("Creating input and output tables");
            Dataset_Source = "DatasetBlobSource";
            Dataset_Destination = "DatasetBlobDestination";

            client.Datasets.CreateOrUpdate(resourceGroupName, dataFactoryName,
                new DatasetCreateOrUpdateParameters()
                {
                    Dataset = new Dataset()
                    {
                        Name = Dataset_Source,
                        Properties = new DatasetProperties()
                        {
                            LinkedServiceName = "LinkedService-AzureStorage",
                            TypeProperties = new AzureBlobDataset()
                            {
                                FolderPath = "sample/",
                                FileName = "input.txt"
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

            client.Datasets.CreateOrUpdate(resourceGroupName, dataFactoryName,
                new DatasetCreateOrUpdateParameters()
                {
                    Dataset = new Dataset()
                    {
                        Name = Dataset_Destination,
                        Properties = new DatasetProperties()
                        {

                            LinkedServiceName = "LinkedService-AzureStorage",
                            TypeProperties = new AzureBlobDataset()
                            {
                                FolderPath = "sample/output/{Slice}",
                                PartitionedBy = new Collection<Partition>()
                    {
                        new Partition()
                        {
                            Name = "Slice",
                            Value = new DateTimePartitionValue()
                            {
                                Date = "SliceStart",
                                Format = "yyyyMMdd-HHmm"
                            }
                        }
                    }
                            },

                            Availability = new Availability()
                            {
                                Frequency = SchedulePeriod.Minute,
                                Interval = 15,
                            },
                        }
                    }
                });
        }
    }
}
