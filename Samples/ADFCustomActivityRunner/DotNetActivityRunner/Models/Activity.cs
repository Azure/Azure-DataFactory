using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class Activity
    {
        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("inputs")]
        internal List<ActivityInput> Inputs { get; set; }

        [JsonProperty("outputs")]
        internal List<ActivityOutput> Outputs { get; set; }

        [JsonProperty("linkedServiceName")]
        internal string LinkedServiceName { get; set; }

        [JsonProperty("typeProperties")]
        internal DotNetActivityTypeProperties DotNetActivityTypeProperties { get; set; }


    }
}