using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public interface ILogger
    {
        void Write(string format, params object[] args);

        void WriteError(Exception e);
    }
}
