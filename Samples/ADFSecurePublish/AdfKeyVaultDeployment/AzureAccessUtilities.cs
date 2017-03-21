using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public class AzureAccessUtilities
    {
        public static async Task<List<DataFactoryInfo>> GetDataFactories(SettingsContext settingsContext)
        {
            var dataFactories = new List<DataFactoryInfo>();

            string uri =
                $"https://management.azure.com/subscriptions/{settingsContext.SubscriptionId}/resources?$filter=resourceType%20eq%20'Microsoft.DataFactory%2Fdatafactories'&api-version=2014-04-01-preview";
            HttpResponseMessage response = await ExecuteArmRequest(settingsContext, HttpMethod.Get, uri);

            string responseText = response.Content.ReadAsStringAsync().Result;

            var jObject = JObject.Parse(responseText);

            var itemArray = jObject["value"] as JArray;

            if (itemArray != null)
            {
                foreach (JToken jToken in itemArray)
                {
                    JObject item = jToken as JObject;

                    string id = item?["id"].ToObject<string>();
                    string resourceGroup = string.Empty;

                    if (!string.IsNullOrEmpty(id))
                    {
                        resourceGroup = id.Split('/')[4];
                    }

                    DataFactoryInfo dfi = new DataFactoryInfo
                    {
                        SubscriptionId = settingsContext.SubscriptionId,
                        Location = item?["location"].ToObject<string>(),
                        Name = item?["name"].ToObject<string>(),
                        ResourceGroup = resourceGroup
                    };

                    dataFactories.Add(dfi);
                }
            }

            return dataFactories;
        }

        public static async Task<HttpResponseMessage> ExecuteArmRequest(SettingsContext settingsContext, HttpMethod httpMethod, string uri, object requestBody = null)
        {
            Uri baseUrl = new Uri(uri);

            string authorizationToken = GetAuthorizationHeaderNoPopup(settingsContext);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, baseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorizationToken);

            if (requestBody != null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = await client.SendAsync(request);

            return response;
        }

        /// <summary>TokenCloudCredentials 
        /// Gets the authorization header without popup.
        /// </summary>
        public static string GetAuthorizationHeaderNoPopup(SettingsContext settingsContext)
        {
            var authority = new Uri(new Uri("https://login.windows.net"), settingsContext.ActiveDirectoryTenantId);
            var context = new AuthenticationContext(authority.AbsoluteUri);
            var credential = new ClientCredential(settingsContext.AdfClientId, settingsContext.AdfClientSecret);
            
            AuthenticationResult result = context.AcquireTokenAsync(settingsContext.WindowsManagementUri, credential).Result;
            if (result != null)
            {
                return result.AccessToken;
            }

            throw new InvalidOperationException("Failed to acquire token");
        }
    }
}
