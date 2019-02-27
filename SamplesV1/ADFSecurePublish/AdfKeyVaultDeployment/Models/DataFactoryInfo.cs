using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models
{
    public class DataFactoryInfo
    {
        public string Name { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }

        public string Location { get; set; }
    }
}
