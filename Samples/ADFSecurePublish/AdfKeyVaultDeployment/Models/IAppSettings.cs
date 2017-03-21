using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models
{
    public interface IAppSettings
    {
        List<Subscription> Subscriptions { get; set; }

        string KeyVaultCertThumbprint { get; set; }

        string KeyVaultCertClientId { get; set; }

        List<EnvironmentSettings> EnvironmentSettings { get; set; }

        string AdfClientId { get; set; }

        string AzureTenantId { get; set; }

        string WindowsManagementUri { get; set; }

        string[] FilesToExclude { get; set; }
    }
}
