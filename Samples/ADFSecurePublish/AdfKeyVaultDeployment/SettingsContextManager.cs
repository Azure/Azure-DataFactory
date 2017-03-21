using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public class SettingsContextManager : ISettingsContextManager
    {
        private AppSettings settings;

        public SettingsContextManager()
        {
        }

        public SettingsContextManager(AppSettings settings)
        {
            this.settings = settings;
        }

        public SettingsContext GetSettingsContext(string environment)
        {
            var environmentSetting = settings.EnvironmentSettings.First(x => x.Name == environment);

            SettingsContext settingsContext = new SettingsContext
            {
                AdfClientId = settings.AdfClientId,
                KeyVaultCertClientId = settings.KeyVaultCertClientId,
                KeyVaultCertificateThumbprint = settings.KeyVaultCertThumbprint,
                ActiveDirectoryTenantId = settings.AzureTenantId,
                WindowsManagementUri = settings.WindowsManagementUri,
                KeyVaultName = environmentSetting.KeyVaultName,
                KeyVaultDnsSuffix = string.IsNullOrEmpty(environmentSetting.KeyVaultDnsSuffix) ? "vault.azure.net:443" : environmentSetting.KeyVaultDnsSuffix,
                DeploymentConfigName = environmentSetting.DeploymentConfigName
            };

            return settingsContext;
        }
        
    }
}
