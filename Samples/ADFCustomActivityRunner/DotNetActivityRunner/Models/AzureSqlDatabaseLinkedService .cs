using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class AzureSqlDatabaseLinkedService : LinkedService
    {
        [JsonProperty("properties")]
        internal StorageServiceProperties Properties { get; set; }
    }
}
