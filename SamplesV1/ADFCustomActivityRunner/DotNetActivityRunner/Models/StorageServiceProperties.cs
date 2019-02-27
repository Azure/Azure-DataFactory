using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class StorageServiceProperties
    {
        [JsonProperty("typeProperties")]
        internal StorageServiceTypeProperties TypeProperties { get; set; }
    }
}