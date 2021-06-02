using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using SecurePublishForm;

namespace SecurePublishMenuCommand.UserSettings
{
    public class SettingsPageGrid : DialogPage, IAppSettings
    {
        [Category("Settings")]
        [DisplayName("Subscriptions")]
        [Description("The subscriptions which target Data Factories reside. Enter a friendly name and ID for each target subscription.")]
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        [Category("Settings")]
        [DisplayName("KeyVault Certificate Thumbprint")]
        [Description("This is the thumbprint of a certificate which has been registered with all Key Vaults which Secure Publish accesses. It also needs to be installed on the local machine in order to access these Key Vaults.")]
        public string KeyVaultCertThumbprint { get; set; }

        [Category("Settings")]
        [DisplayName("KeyVault Certificate Client ID")]
        [Description("This is the client ID of an Azure Active Directory application which has the certificate associated with it.")]
        public string KeyVaultCertClientId { get; set; }

        [Category("Settings")]
        [DisplayName("Environment Settings")]
        [Description("The environment settings, consisting of; Environment Name, associated Key Vault name and DNS suffix, and associated deployment config (if one exists).")]
        public List<EnvironmentSettings> EnvironmentSettings { get; set; } = new List<EnvironmentSettings>();

        // TODO: Consolodate this AAD client ID with the other AAD CLient ID
        [Category("Settings")]
        [DisplayName("ADF AAD Client ID")]
        [Description("This is the client ID of an Azure Active Directory application which has been associated with the subscription which has data factories that we want to publish to.")]
        public string AdfClientId { get; set; }

        [Category("Settings")]
        [DisplayName("Azure Tenant ID")]
        [Description("This is the azure tenant ID associated with your account.")]
        public string AzureTenantId { get; set; }

        [Category("Settings")]
        [DisplayName("Windows Management Uri")]
        public string WindowsManagementUri { get; set; } = "https://management.core.windows.net/";

        [Category("Settings")]
        [DisplayName("Files to Exclude")]
        [Description("The name of files which will be excluded from the deployment to Azure.")]
        public string[] FilesToExclude { get; set; }

        readonly string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SecurePublishSettings.json");

        public override void SaveSettingsToStorage()
        {
            var appSettingsType = typeof(AppSettings);
            var thisType = GetType();

            var settings = new AppSettings();

            // copy properties
            foreach (var property in thisType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                var propertyToSet = appSettingsType.GetProperty(property.Name);
                propertyToSet.SetValue(settings, property.GetValue(this));
            }

            File.WriteAllText(settingsFile, JsonConvert.SerializeObject(settings));
        }

        public override void LoadSettingsFromStorage()
        {
            if (File.Exists(settingsFile))
            {
                AppSettings settings = null;

                try
                {
                    settings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(settingsFile));
                }
                catch
                {
                    string backUpFile = Path.Combine(Path.GetDirectoryName(settingsFile),
                        Path.GetFileNameWithoutExtension(settingsFile) + "_BackUp.json");
                    File.Move(settingsFile, backUpFile);

                    return;
                }

                var appSettingsType = typeof(AppSettings);
                var thisType = GetType();

                // copy properties
                foreach (var property in appSettingsType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    var propertyToSet = thisType.GetProperty(property.Name);
                    propertyToSet.SetValue(this, property.GetValue(settings));
                }
            }
        }
    }
}
