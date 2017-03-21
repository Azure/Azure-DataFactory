using System;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models
{
    /// <summary>
    /// Information on custom activities 
    /// </summary>
    public class CustomActivityPackageInfo : IEquatable<CustomActivityPackageInfo>
    {
        public string PackageLinkedService { get; set; }

        public string PackageFile { get; set; }

        public bool Equals(CustomActivityPackageInfo other)
        {
            if (PackageLinkedService == other?.PackageLinkedService &&
                PackageFile == other?.PackageFile)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hashPackageLinkedService = PackageLinkedService == null ? 0 : PackageLinkedService.GetHashCode();
            int hashPackageFile = PackageFile == null ? 0 : PackageFile.GetHashCode();

            return hashPackageLinkedService ^ hashPackageFile;
        }
    }
}
