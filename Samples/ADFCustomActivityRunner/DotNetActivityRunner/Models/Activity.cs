using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class Activity
    {
        [JsonProperty("name")]
        public string Name { get; internal set; }

        [JsonProperty("inputs")]
        public List<ActivityInput> Inputs { get; internal set; }

        [JsonProperty("outputs")]
        public List<ActivityOutput> Outputs { get; internal set; }

        [JsonProperty("linkedServiceName")]
        public string LinkedServiceName { get; internal set; }

        [JsonProperty("typeProperties")]
        public DotNetActivityTypeProperties DotNetActivityTypeProperties { get; internal set; }


    }
}