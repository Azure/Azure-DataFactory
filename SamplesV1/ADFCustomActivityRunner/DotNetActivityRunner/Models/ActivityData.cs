using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class ActivityData
    {
        [JsonProperty("name")]
        internal string Name { get; set; }
    }
}