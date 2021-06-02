namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models
{
    public class EnvironmentSettings
    {
        public string Name { get; set; }

        public string KeyVaultName { get; set; }

        public string KeyVaultDnsSuffix { get; set; }

        public string DeploymentConfigName { get; set; }
    }
}
