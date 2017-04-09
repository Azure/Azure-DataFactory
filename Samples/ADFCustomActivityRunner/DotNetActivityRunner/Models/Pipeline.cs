using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class Pipeline
    {
        [JsonProperty("properties")]
        public PipelineProperties Properties { get; internal set; }
    }
}