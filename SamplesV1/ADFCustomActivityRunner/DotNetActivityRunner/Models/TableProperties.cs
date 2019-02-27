using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class TableProperties
    {
        [JsonProperty("linkedServiceName")]
        internal string LinkedServiceName { get; set; }

        [JsonProperty("type")]
        internal string Type { get; set; }

        [JsonProperty("typeProperties")]
        internal TableTypeProperties TypeProperties { get; set; }
    }
}