using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public interface IKeyVaultResolver
    {
        string KeyVaultName { get; set; }

        Task<Secret> GetSecret(string identifier);
    }
}
