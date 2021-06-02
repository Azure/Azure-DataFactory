using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models
{
    public class SettingsContext
    {
        public string SubscriptionId { get; set; }

        public string ActiveDirectoryTenantId { get; set; }

        public string AdfClientId { get; set; }

        public string AdfClientSecret { get; set; }

        public string WindowsManagementUri { get; set; }

        public string KeyVaultName { get; set; }

        public string KeyVaultDnsSuffix { get; set; }

        public string KeyVaultCertificateThumbprint { get; set; }

        public string KeyVaultCertClientId { get; set; }

        public string KeyVaultCertClientSecret { get; set; }

        public string DeploymentConfigName { get; set; }

        public List<string> FilesToExclude { get; set; }
    }
}
