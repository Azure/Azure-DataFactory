using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class PipelineProperties
    {
        [JsonProperty("activities")]
        public List<Activity> Activities { get; set; }
    }
}