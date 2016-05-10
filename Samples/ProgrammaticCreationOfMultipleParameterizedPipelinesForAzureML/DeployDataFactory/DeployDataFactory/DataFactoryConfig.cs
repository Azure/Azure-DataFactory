// Copyright (c) Microsoft Corporation. All Rights Reserved.

using System;

namespace DeployDataFactory
{
    class DataFactoryConfig
    {
        // Data factory config
        public static string DataFactoryName = "<your data factory name>";
        public static string ResourceGroupName = "<your resource group name>";
        public static string DeploymentRegion = "North Europe";

        // Storage account
        public static string StorageAccountName = "<your storage account>";
        public static string StorageAccountKey = "<your storage account key>";

        // Retraining endpoints
        public static string RetrainingEndPoint = @"<your retraining web service endpoint url>";
        public static string RetrainingApiKey = @"<your retraining web service api key>";

        //  This sample uses the same hardwired scoring endpoint for each entry in the list of scoring endpoints.
        // 
        // In reality you want to replace this with code that iterates and creates
        // the required no. of endpoints programmaticaly.
        // 
        // The relevant code for this can be found here:
        // 
        // https://github.com/raymondlaghaeian/AML_EndpointMgmt/blob/master/Program.cs

        public static string ScoringEndPoint = @"<scoring endpoint>";
        public static string ScoringApiKey = @"<scoring endpoint apikey>";
        public static string ScoringUpdateResourceEndPoint = @"<scoring endpoint update reource endpoint>";
        public static string ScoringLinkedServiceNamePrefix = "LinkedServiceScoring-AzureML-";

        // Pipeline schedule config
        public static string PipelineStartTime = "06/22/2015";
        public static int MinutesToAddToStartTimeForEndTime = 60;
        public static uint PipelineFrequencyInMinutes = 15;
    }
}
