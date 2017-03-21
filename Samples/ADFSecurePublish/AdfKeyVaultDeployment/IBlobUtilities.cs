using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public interface IBlobUtilities
    {
        Task<bool> UploadFile(string localFilePath, string connectionString, string folderPath);
    }
}
