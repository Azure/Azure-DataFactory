using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class PipelineProperties
    {
        [JsonProperty("activities")]
        internal List<Activity> Activities { get; set; }
    }
}