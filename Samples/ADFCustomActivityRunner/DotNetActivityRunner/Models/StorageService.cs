using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class StorageService : LinkedService
    {
        [JsonProperty("properties")]
        public StorageServiceProperties Properties { get; internal set; }
    }
}