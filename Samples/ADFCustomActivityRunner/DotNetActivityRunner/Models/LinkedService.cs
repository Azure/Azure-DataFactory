using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{ 
    public class LinkedService
    {
        [JsonProperty("name")]
        public string Name { get; internal set; }

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