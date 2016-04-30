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
        public static void CreateObjects(string[] parameters, DataFactoryManagementClient client)
        {
            string Table_Source;
            string Table_Destination;
            CreateInputOutputTables(DataFactoryConfig.ResourceGroupName, DataFactoryConfig.DataFactoryName, client, out Table_Source, out Table_Destination);

            CreatePipelines(DataFactoryConfig.ResourceGroupName, DataFactoryConfig.DataFactoryName, client, Table_Source, Table_Destination, parameters);
        }

        private static void CreatePipelines(string resourceGroupName, string dataFactoryName, DataFactoryManagementClient client, string Table_Source, string Table_Destination, string[] parameters)
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

                                // Initial value for pipeline's active period. With this, you won't need to set slice status
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
                                Name = Table_Source
                            }
                        },
                        Outputs = new List<ActivityOutput>()
                        {
                            new ActivityOutput()
                            {
                                Name = Table_Destination
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

        private static void CreateInputOutputTables(string resourceGroupName, string dataFactoryName, DataFactoryManagementClient client, out string Table_Source, out string Table_Destination)
        {
            // create input and output tables
            Console.WriteLine("Creating input and output tables");
            Table_Source = "TableBlobSource";
            Table_Destination = "TableBlobDestination";

            client.Datasets.CreateOrUpdate(resourceGroupName, dataFactoryName,
                new DatasetCreateOrUpdateParameters()
                {
                    Dataset = new Dataset()
                    {
                        Name = Table_Source,
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
                        Name = Table_Destination,
                        Properties = new DatasetProperties()
                        {

                            LinkedServiceName = "LinkedService-AzureStorage",
                            TypeProperties = new AzureBlobDataset()
                            {
                                FolderPath = "genscapesample/output/{Slice}",
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
