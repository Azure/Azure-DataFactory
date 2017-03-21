using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string requestUri);

        void Dispose();
    }
}
