using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class AzureSqlDatabaseLinkedService : LinkedService
    {
        [JsonProperty("properties")]
        public StorageServiceProperties Properties { get; internal set; }
    }
}
