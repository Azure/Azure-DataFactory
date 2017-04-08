using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class Table
    {
        [JsonProperty("properties")]
        public TableProperties Properties { get; internal set; }

    }
}