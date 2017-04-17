using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class StorageServiceTypeProperties
    {
        [JsonProperty("connectionString")]
        internal string ConnectionString { get; set; }
    }
}