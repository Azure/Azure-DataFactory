using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class TableProperties
    {
        [JsonProperty("linkedServiceName")]
        public string LinkedServiceName { get; internal set; }

        [JsonProperty("type")]
        public string Type { get; internal set; }

        [JsonProperty("typeProperties")]
        public TableTypeProperties TypeProperties { get; internal set; }
    }
}