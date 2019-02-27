using System.Collections.Generic;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models
{
    public class AppSettings : IAppSettings
    {
        public List<Subscription> Subscriptions { get; set; }

        public string KeyVaultCertThumbprint { get; set; }

        public string KeyVaultCertClientId { get; set; }

        public List<EnvironmentSettings> EnvironmentSettings { get; set; }

        public string AdfClientId { get; set; }

        public string AzureTenantId { get; set; }

        public string WindowsManagementUri { get; set; } 

        public string[] FilesToExclude { get; set; }
    }
}
