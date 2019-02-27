using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;
using Newtonsoft.Json;
using Path = System.IO.Path;

namespace SecurePublishForm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILogger
    {
        private SettingsContextManager settingsContextManager;
        private List<DataFactoryInfo> dataFactoryList;
        private string environment;
        private string dataFactory;
        private string projName;
        private PublishManager publishManager;
        private AppSettings settings;

        public string ControlVisiblity { get; set; }

        private string enterSettingsMsg = "Please close this form and enter the settings through Tools -> Options -> Data Factory -> Secure Publish";

        public MainWindow(string projName)
        {
            this.projName = projName;
            InitializeComponent();

            dataFactoryListBox.IsEnabled = false;
            publishButton.IsEnabled = false;

            LoadSettings();

            this.DataContext = this;
        }

        private void LoadSettings()
        {
            string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "SecurePublishSettings.json");
            if (File.Exists(settingsFile))
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(settingsFile));
                    ControlVisiblity = "Visible";
                    noSettings.Visibility = Visibility.Hidden;
                }
                catch
                {
                    ShowNoSettings();
                    string backUpFile = Path.Combine(Path.GetDirectoryName(settingsFile),
                        Path.GetFileNameWithoutExtension(settingsFile) + "_BackUp.json");
                    File.Move(settingsFile, backUpFile);
                    noSettings.Content =
                        $"There was a problem reading your user settings. The user settings file is corrupted.\r\nA backup has been saved to {backUpFile}. Please re-enter them again.\r\n" +
                        enterSettingsMsg;

                    return;
                }

                List<string> validationErrors = ValidateSettings();

                if (validationErrors.Any())
                {
                    ShowNoSettings();
                    noSettings.Content =
                        $"There was a problem reading your user settings.\r\n{string.Join("\r\n", validationErrors)}\r\n{enterSettingsMsg}";
                    return;
                }

                settingsContextManager = new SettingsContextManager(settings);

                subscriptionList.ItemsSource = settings.Subscriptions.Select(x => x.FriendlyName).ToList();
                subscriptionList.SelectedIndex = 0;
            }
            else
            {
                ShowNoSettings();
                noSettings.Content = "No user settings were found. " + enterSettingsMsg;
            }
        }

        private List<string> ValidateSettings()
        {
            var validationErrors = new List<string>();

            if (settings.Subscriptions == null || !settings.Subscriptions.Any())
            {
                validationErrors.Add("No subscriptions were found.");
            }

            if (settings.EnvironmentSettings == null || !settings.EnvironmentSettings.Any())
            {
                validationErrors.Add("No environments were found.");
            }

            if (string.IsNullOrEmpty(settings.AdfClientId))
            {
                validationErrors.Add("The ADF AAD Client ID setting is empty.");
            }

            if (string.IsNullOrEmpty(settings.KeyVaultCertClientId))
            {
                validationErrors.Add("The KeyVault Certificate Client ID setting is empty.");
            }

            if (string.IsNullOrEmpty(settings.AzureTenantId))
            {
                validationErrors.Add("The Azure Tenant ID setting is empty.");
            }

            if (string.IsNullOrEmpty(settings.KeyVaultCertThumbprint))
            {
                validationErrors.Add("The KeyVault Certificate Thumbprint setting is empty.");
            }

            return validationErrors;
        }

        private void ShowNoSettings()
        {
            noSettings.Visibility = Visibility.Visible;
            ControlVisiblity = "Hidden";
        }

        private async void subscriptionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            publishButton.IsEnabled = false;
            environmentList.ItemsSource = settings.EnvironmentSettings.Select(x => x.Name);

            if (environmentList.SelectedIndex == 0)
            {
                await RefreshDatafactories();
            }
            else
            {
                environmentList.SelectedIndex = 0;
            }
        }

        private async void environmentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await RefreshDatafactories();
        }

        private async Task RefreshDatafactories()
        {
            SettingsContext settingsContext;
            environment = environmentList.SelectedItem as string;

            try
            {
                settingsContext = settingsContextManager.GetSettingsContext(environment);

                // Get KeyVault resolver which is used to retreive keyvault secrets based on environment context
                IKeyVaultResolver keyVault;
                if (!string.IsNullOrEmpty(settingsContext.KeyVaultCertificateThumbprint))
                {
                    var cert = KeyVaultResolver.FindCertificateByThumbprint(settingsContext.KeyVaultCertificateThumbprint);

                    if (cert == null)
                    {
                        Write($"No cert was found using thumbprint {settingsContext.KeyVaultCertificateThumbprint}", "Red");
                        return;
                    }

                    keyVault = new KeyVaultResolver(settingsContext.KeyVaultName, settingsContext.KeyVaultDnsSuffix, settingsContext.KeyVaultCertClientId, cert);
                }
                else
                {
                    keyVault = new KeyVaultResolver(settingsContext.KeyVaultName, settingsContext.KeyVaultDnsSuffix, settingsContext.KeyVaultCertClientId, settingsContext.KeyVaultCertClientSecret);
                }

                settingsContext.SubscriptionId = settings.Subscriptions[subscriptionList.SelectedIndex].Id;
                try
                {
                    settingsContext.AdfClientSecret = (await keyVault.GetSecret("SecurePublishAdfClientSecret")).Value;
                }
                catch (Exception ex)
                {
                    Write($"The secret called SecurePublishAdfClientSecret was not found in the KeyVault '{settingsContext.KeyVaultName}'. The ADF Client Secret is a password which was associated with the AAD Client ID '{settingsContext.KeyVaultCertClientId}' when it was originally set up. If you are setting up a new KeyVault and a previous KeyVault has already been used, you can get this value from the previous KeyVault. Otherwise refer to the user documentation on creating a new client ID and associating it with your Azure subscription.", "Red");
                    WriteError(ex);
                    return;
                }

                publishManager = new PublishManager(keyVault, settingsContext, this);

                dataFactoryList = await AzureAccessUtilities.GetDataFactories(settingsContext);
            }
            catch (Exception e)
            {
                Write(e.Message, "Red");

                Dispatcher.Invoke(() =>
                {
                    dataFactoryListBox.IsEnabled = false;
                });

                return;
            }

            if (!dataFactoryList.Any())
            {
                Write("No data factories found in subscription: " + settingsContext.SubscriptionId, "Orange");
                Write($"They either do not exist or else you may need to associate the Client ID '{settingsContext.AdfClientId}' with the subscription. To do that, perform the following steps:", "Orange");
                Write("1. Open up PowerShell", "Orange");
                Write("2. Log in to Azure by typing in the cmd: Login-AzureRmAccount", "Orange");
                Write($"3. Change to the subscription you wish to use by typing the cmd: Select-AzureRmSubscription -SubscriptionId '{settingsContext.SubscriptionId}'", "Orange");
                Write("4. Associate the Client ID with the Data Factory Contributer role in the current subscription by typing:", "Orange");
                Write($"New-AzureRmRoleAssignment -RoleDefinitionName 'Data Factory Contributor' -ServicePrincipalName '{settingsContext.AdfClientId}'", "Orange");

                Dispatcher.Invoke(() =>
                {
                    dataFactoryListBox.IsEnabled = false;
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    dataFactoryListBox.IsEnabled = true;
                });
            }

            Dispatcher.Invoke(() =>
            {
                dataFactoryListBox.ItemsSource = dataFactoryList.Select(x => x.Name);
            });
        }

        private void dataFactoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            publishButton.IsEnabled = true;
            dataFactory = dataFactoryListBox.SelectedItem as string;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void publishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                publishButton.IsEnabled = false;
                Write("Starting Secure Publish", "Black");
                Write(string.Empty);

                DataFactoryInfo choosenDataFactory = dataFactoryList.Single(x => x.Name == dataFactory);

                await publishManager.BuildAndSecurePublish(projName, choosenDataFactory);
            }
            catch (Exception ex)
            {
                Write("An error occurred: " + ex.Message, "Red");
                WriteError(ex);
            }

            publishButton.IsEnabled = true;
        }

        public void WriteError(Exception exception)
        {
            try
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Secure Publish";
                    eventLog.WriteEntry(exception.ToString(), EventLogEntryType.Error);
                }
            }
            catch
            {
                //  Output to the form if there was a problem accessing the event log
                Write(exception.ToString(), "Red");
            }
        }

        public void Write(string format, params object[] args)
        {
            Dispatcher.Invoke(() =>
            {
                if (args != null && args.Length == 1)
                {
                    string color = (string)args[0];

                    BrushConverter bc = new BrushConverter();
                    TextRange tr = new TextRange(outputBox.Document.ContentEnd, outputBox.Document.ContentEnd)
                    {
                        Text = format
                    };

                    try
                    {
                        tr.ApplyPropertyValue(TextElement.ForegroundProperty, bc.ConvertFromString(color));
                        tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                    }
                    catch (FormatException)
                    {
                        TextRange tr2 = new TextRange(outputBox.Document.ContentEnd, outputBox.Document.ContentEnd)
                        {
                            Text = format
                        };

                        tr2.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                        tr2.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                    }
                }
                else
                {
                    TextRange tr = new TextRange(outputBox.Document.ContentEnd, outputBox.Document.ContentEnd)
                    {
                        Text = format
                    };

                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                    tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                }

                outputBox.AppendText("\u2028"); // Linebreak, not paragraph break
                outputBox.ScrollToEnd();
            });
        }
    }
}
