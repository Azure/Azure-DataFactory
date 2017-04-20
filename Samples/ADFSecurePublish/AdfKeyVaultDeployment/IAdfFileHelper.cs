using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public interface IAdfFileHelper
    {
        DeployConfigInfo GetDeployConfigInfo(string filePath);

        Task<AdfFileInfo> GetFileInfo(string filePath, DeployConfigInfo deployConfig = null);

        Task GetSchemas(IHttpClient httpClient);
    }
}
