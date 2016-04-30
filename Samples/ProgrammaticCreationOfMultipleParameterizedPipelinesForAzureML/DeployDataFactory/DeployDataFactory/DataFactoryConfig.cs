using System;

namespace DeployDataFactory
{
    class DataFactoryConfig
    {
        // Data factory config
        public static string DataFactoryName = "ParameterizedDataFactory";
        public static string ResourceGroupName = "ADF";
        public static string DeploymentRegion = "northeu";

        // Storage account
        public static string StorageAccountName = "hirensmontest";
        public static string StorageAccountKey = "aXSYzsAKzLZYFA8zfEdBSmv63hvpuzLTFH9c/0OKCDVqPM3lDb5MSFlk52Ds1IxuLYyvLZhmSV+m40KKSeFaLQ==";

        // Retraining endpoints
        public static string RetrainingEndPoint = @"https://ussouthcentral.services.azureml.net/workspaces/aac49c5151fa40abbc206711d502a9c5/services/ab7157832ac84a54b5691413cd62b353/jobs?api-version=2.0";
        public static string RetrainingApiKey = @"Pt8H4jWNR26nmT7858n2FoesZjahf9FhGS9os3hi36r2GX7SGxskpuwHckKIWMULibQ8k3pYt9F3wTfSu4QiAw==";

        // Scoring endpoint templates
        public static string ScoringEndPoint = @"https://ussouthcentral.services.azureml.net/workspaces/aac49c5151fa40abbc206711d502a9c5/services/751c1a1b149e4fba9a08bbb0f21cdaec/jobs?api-version=2.0";
        public static string ScoringApiKey = @"o8gGBGgf33OUiC6ed65N7Pw6SmTg0/VefjbrVaLa4I8t2bWhQ65KzWenw2lszQ8NS7mwk5ibsnmhjCmHk6AAew==";
        public static string ScoringUpdateResourceEndPoint = @"https://management.azureml.net/workspaces/aac49c5151fa40abbc206711d502a9c5/services/751c1a1b149e4fba9a08bbb0f21cdaec/update-resource";
        public static string ScoringLinkedServiceNamePrefix = "LinkedServiceScoring-AzureML-";

        // Pipeline schedule config
        public static string PipelineStartTime = "06/22/2015";
        public static int MinutesToAddToStartTimeForEndTime = 60;
        public static uint PipelineFrequencyInMinutes = 15;
    }
}
