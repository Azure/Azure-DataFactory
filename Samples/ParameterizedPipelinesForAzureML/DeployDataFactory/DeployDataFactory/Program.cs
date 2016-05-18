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
    class Program
    {
        static void Main(string[] args)
        {
            // Read region values from parameters.txt file. 
            // We will create on retraining pipeline and one scoring pipeline for each region values.
            // Note: You will typically replace this with code to read this from your data store.
            string[] parameters =  System.IO.File.ReadAllLines(@"parameters.txt");

            // create data factory management client. This will pop up a UI for 
            // Azure login for your subscription.
            TokenCloudCredentials aadTokenCredentials =
                new TokenCloudCredentials(
                    ConfigurationManager.AppSettings["SubscriptionId"],
                    Utilities.GetAuthorizationHeader());

            Uri resourceManagerUri = new Uri(ConfigurationManager.AppSettings["ResourceManagerEndpoint"]);

            DataFactoryManagementClient client = new DataFactoryManagementClient(aadTokenCredentials, resourceManagerUri);

            Utilities.CreateDataFactory(DataFactoryConfig.ResourceGroupName, DataFactoryConfig.DataFactoryName, client);

            // We use the same storage account to put results of both retraining and scoring. Hence create the shared
            // linked servive for the storage account before creating the 2 pipelines.
            Utilities.CreateStorageLinkedService(DataFactoryConfig.ResourceGroupName, DataFactoryConfig.DataFactoryName, client);

            // Note: The CreateMLEndpoints creates as many endpoint entries in the endpoints collection as there are regions.
            // In this sample we are using the same hardwired value of the endpoint for each entry in the collection. You will need
            // to replace this code with code that programatically creates multiple endpoints.
            IList<UpdateResourceEndpoint> mlEndpoints;
            Utilities.CreateMLEndpoints(out mlEndpoints, parameters.Length);
            
            // Note: retraining pipeline generation code creates the scoring linked services that are referenced by 
            // scoring pipeline. Hence retraining pipeline code MUST run before the scoring pipeline code.            
            RetrainingPipeline.CreateObjects(parameters, client, mlEndpoints);
            ScoringPipeline.CreateObjects(parameters, client);

            // We don't have a copy step. This is commented out bonus code for copying data if you needed to have data movement as well.
            //CopyPipeline.CreateObjects(parameters, client);

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }

    }
}
