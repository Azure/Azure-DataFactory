using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class DotNetActivityTypeProperties
    {
        [JsonProperty("extendedProperties")]
        internal Dictionary<string, string> ExtendedProperties { get; set; }
    }
}
