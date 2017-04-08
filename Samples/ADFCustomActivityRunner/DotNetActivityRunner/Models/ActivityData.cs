using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class ActivityData
    {
        [JsonProperty("name")]
        public string Name { get; internal set; }
    }
}