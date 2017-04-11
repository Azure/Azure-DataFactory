using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class Pipeline
    {
        [JsonProperty("properties")]
        internal PipelineProperties Properties { get; set; }
    }
}