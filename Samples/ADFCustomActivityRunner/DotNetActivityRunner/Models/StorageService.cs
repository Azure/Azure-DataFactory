using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class StorageService : LinkedService
    {
        [JsonProperty("properties")]
        internal StorageServiceProperties Properties { get; set; }
    }
}