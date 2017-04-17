namespace AzureAnalysisServicesProcessSample
{ 
    using System;

    [Serializable]
    public class ProcessAzureASContext
    {
        public string TabularDatabaseName { get; set; }
        public string AzureASConnectionString { get; set; }
    }
}
