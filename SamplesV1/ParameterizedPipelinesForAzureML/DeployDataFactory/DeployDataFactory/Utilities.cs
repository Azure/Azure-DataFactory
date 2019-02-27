using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;
using System.Collections.ObjectModel;

using Microsoft.Azure.Management.DataFactories;
// Copyright (c) Microsoft Corporation. All Rights Reserved.

using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.Azure.Management.DataFactories.Common.Models;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure;

namespace DeployDataFactory
{
    class Utilities
    {
        /// <summary>
        /// Creates a data factory with a given name, resource group in the specified data factory region
        /// </summary>
        public static void CreateDataFactory(string resourceGroupName, string dataFactoryName, DataFactoryManagementClient client)
        {
            // create a data factory
            Console.WriteLine("Creating a data factory");
            client.DataFactories.CreateOrUpdate(resourceGroupName,
                new DataFactoryCreateOrUpdateParameters()
                {
                    DataFactory = new DataFactory()
                    {
                        Name = dataFactoryName,
                        Location = DataFactoryConfig.DeploymentRegion,
                        Properties = new DataFactoryProperties() { }
                    }
                }
            );
        }

        /// <summary>
        /// Create the storage linked service. The same storage account is used for both retraining and scoring outputs. 
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="dataFactoryName"></param>
        /// <param name="client"></param>
        public static void CreateStorageLinkedService(string resourceGroupName, string dataFactoryName, DataFactoryManagementClient client)
        {
            // create a linked service
            Console.WriteLine("Creating a linked service");
            client.LinkedServices.CreateOrUpdate(resourceGroupName, dataFactoryName,
                new LinkedServiceCreateOrUpdateParameters()
                {
                    LinkedService = new LinkedService()
                    {
                        Name = "LinkedService-AzureStorage",
                        Properties = new LinkedServiceProperties
                        (
                            new AzureStorageLinkedService(String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", DataFactoryConfig.StorageAccountName, DataFactoryConfig.StorageAccountKey))
                        )
                    }
                }
            );
        }

        /// <summary>
        /// The following function takes the requested no. of endpoints and returns a
        /// list with corresponding no. of endpoints.
        /// 
        /// The function uses the same hardwired endpoint for each entry in the list
        /// 
        /// In reality you want to replace this with code that iterates and creates
        /// the required no. of endpoints programmaticaly.
        /// 
        /// The relevant code for this can be found here:
        /// 
        /// https://github.com/raymondlaghaeian/AML_EndpointMgmt/blob/master/Program.cs
        /// </summary>
        /// <param name="mlEndpoints">List of ML endpoints created</param>
        /// <param name="count">No. of ml endpoints to create</param>
        public static void CreateMLEndpoints(out IList<UpdateResourceEndpoint> mlEndpoints, int count)
        {
            mlEndpoints = new List<UpdateResourceEndpoint>();

            for (int i = 0; i < count; i++)
            {
                UpdateResourceEndpoint endpoint = new UpdateResourceEndpoint();
                endpoint.mlEndpoint = DataFactoryConfig.ScoringEndPoint;
                endpoint.apiKey = DataFactoryConfig.ScoringApiKey;
                endpoint.updateResourceEndpointUrl = DataFactoryConfig.ScoringUpdateResourceEndPoint;
                ;
                mlEndpoints.Add(endpoint);
            }
        }


        public static string GetAuthorizationHeader()
        {
            AuthenticationResult result = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var context = new AuthenticationContext(ConfigurationManager.AppSettings["ActiveDirectoryEndpoint"] + ConfigurationManager.AppSettings["ActiveDirectoryTenantId"]);

                    result = context.AcquireToken(
                        resource: ConfigurationManager.AppSettings["WindowsManagementUri"],
                        clientId: ConfigurationManager.AppSettings["AdfClientId"],
                        redirectUri: new Uri(ConfigurationManager.AppSettings["RedirectUri"]),
                        promptBehavior: PromptBehavior.Always);
                }
                catch (Exception threadEx)
                {
                    Console.WriteLine(threadEx.Message);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Name = "AcquireTokenThread";
            thread.Start();
            thread.Join();

            if (result != null)
            {
                return result.AccessToken;
            }

            throw new InvalidOperationException("Failed to acquire token");
        }
        public static string GetScoringLinkedServiceName(string scoringLinkedServiceNamePrefix, string region)
        {
            return String.Format("{0}{1}", scoringLinkedServiceNamePrefix, region);
        }
    }
}
