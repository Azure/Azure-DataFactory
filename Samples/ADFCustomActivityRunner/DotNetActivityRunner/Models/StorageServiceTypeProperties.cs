using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class StorageServiceTypeProperties
    {
        [JsonProperty("connectionString")]
        public string ConnectionString { get; internal set; }
    }
}