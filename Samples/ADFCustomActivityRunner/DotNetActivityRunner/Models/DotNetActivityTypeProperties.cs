using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    public class DotNetActivityTypeProperties
    {
        [JsonProperty("extendedProperties")]
        public Dictionary<string, string> ExtendedProperties { get; internal set; }
    }
}
