using System.Collections.Generic;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models
{
    /// <summary>
    /// Information on deplyment configuration fies
    /// </summary>
    public class DeployConfigInfo
    {
        public string FilePath { get; set; }

        public string FileName { get; set; }

        public Dictionary<string, Dictionary<string, string>> DeploymentDictionary { get; set; }
    }
}
