using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class StorageServiceProperties
    {
        [JsonProperty("typeProperties")]
        public StorageServiceTypeProperties TypeProperties { get; internal set; }
    }
}