using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class Table
    {
        [JsonProperty("properties")]
        internal TableProperties Properties { get; set; }

    }
}