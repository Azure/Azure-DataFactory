using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models
{
    /// <summary>
    /// Information on ADF Files such as pipelines, linked services and tables (datasets)
    /// </summary>
    public class AdfFileInfo
    {
        public string FileName { get; set; }

        public bool IsValid { get; set; }

        public string Name { get; set; }

        public string FileContents { get; set; }

        public FileType FileType { get; set; }

        public JObject JObject { get; set; }

        public string SubType { get; set; }

        public Exception ErrorException { get; set; }

        public List<CustomActivityPackageInfo> CustomActivityPackages { get; set; } 
    }
}
