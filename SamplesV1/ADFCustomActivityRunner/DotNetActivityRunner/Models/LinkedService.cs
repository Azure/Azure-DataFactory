using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class LinkedService
    {
        [JsonProperty("name")]
        internal string Name { get; set; }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Name.Equals((obj as LinkedService).Name);
        }
    }
}