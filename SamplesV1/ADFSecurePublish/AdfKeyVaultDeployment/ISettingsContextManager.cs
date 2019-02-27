using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public interface ISettingsContextManager
    {
        SettingsContext GetSettingsContext(string environment);
    }
}
