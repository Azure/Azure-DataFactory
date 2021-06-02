using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public class KeyVaultResolver : IKeyVaultResolver
    {
        public string KeyVaultName { get; set; }
        private string keyVaultDnsSuffix { get; set; }

        private string keyVaultClientId;
        private string keyVaultSecret;
        private KeyVaultClient client;
        private X509Certificate2 keyVaultCert;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultResolver"/> class using client secret.
        /// </summary>
        /// <param name="keyVaultName">Name of the key vault.</param>
        /// <param name="keyVaultDnsSuffix">DNS suffix of the key vault.</param>
        /// <param name="keyVaultClientId">The key vault client identifier.</param>
        /// <param name="keyVaultSecret">The key vault secret.</param>
        public KeyVaultResolver(string keyVaultName, string keyVaultDnsSuffix, string keyVaultClientId, string keyVaultSecret)
        {
            KeyVaultName = keyVaultName;
            this.keyVaultDnsSuffix = keyVaultDnsSuffix;
            this.keyVaultClientId = keyVaultClientId;
            this.keyVaultSecret = keyVaultSecret;

            client = new KeyVaultClient(GetToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultResolver"/> class using the associated certificated.
        /// </summary>
        /// <param name="keyVaultName">Name of the key vault.</param>
        /// <param name="keyVaultDnsSuffix">DNS suffix of the key vault.</param>
        /// <param name="keyVaultClientId">The key vault client identifier.</param>
        /// <param name="keyVaultCert">The key vault cert.</param>
        public KeyVaultResolver(string keyVaultName, string keyVaultDnsSuffix, string keyVaultClientId, X509Certificate2 keyVaultCert)
        {
            KeyVaultName = keyVaultName;
            this.keyVaultDnsSuffix = keyVaultDnsSuffix;
            this.keyVaultClientId = keyVaultClientId;
            this.keyVaultCert = keyVaultCert;

            client = new KeyVaultClient(GetTokenUsingCert);
        }

        /// <summary>
        /// Gets the secret from the Key Vault using the identifier.
        /// </summary>
        public async Task<Secret> GetSecret(string identifier)
        {
            string secretIdentifier = $"https://{KeyVaultName}.{keyVaultDnsSuffix}/secrets/{identifier}";

            try
            {
                return await client.GetSecretAsync(secretIdentifier);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to retreive secret identifier '{secretIdentifier}' from Key Vault '{KeyVaultName}'. Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds the certificate by thumbprint.
        /// </summary>
        /// <param name="thumbPrint">The certificate thumbprint.</param>
        public static X509Certificate2 FindCertificateByThumbprint(string thumbPrint)
        {
            X509Certificate2 cert;

            X509Store myLocalStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            cert = FindCertFromStore(thumbPrint, myLocalStore);

            if (cert != null)
            {
                return cert;
            }

            X509Store myCurrentUserStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            cert = FindCertFromStore(thumbPrint, myCurrentUserStore);

            if (cert != null)
            {
                return cert;
            }

            X509Store caLocalStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
            cert = FindCertFromStore(thumbPrint, caLocalStore);

            return cert;
        }

        private static X509Certificate2 FindCertFromStore(string thumbPrint, X509Store store)
        {
            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection col = store.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, false);
                if (col.Count == 0)
                {
                        return null;
                }

                return col[0];
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Gets the token used to connect to Key Vault.
        /// </summary>
        private async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(keyVaultClientId, keyVaultSecret);

            // Note: An exception here can indicate that the local cert has become corrupted. Please first try and install it again.
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }

        /// <summary>
        /// Gets the token using the cert.
        /// </summary>
        /// <param name="authority">The authority.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="scope">The scope.</param>
        public async Task<string> GetTokenUsingCert(string authority, string resource, string scope)
        {
            var assertionCert = new ClientAssertionCertificate(keyVaultClientId, keyVaultCert);

            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var result = await context.AcquireTokenAsync(resource, assertionCert);
            return result.AccessToken;
        }
    }
}
