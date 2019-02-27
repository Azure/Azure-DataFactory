using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    /// <summary>
    /// This class is used to perform a build on the selected ADF project.
    /// </summary>
    public class AdfBuild
    {
        private ILogger logger;

        public AdfBuild(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Builds the project from specified project path.
        /// </summary>
        /// <param name="projPath">The project path.</param>
        /// <param name="buildType">Type of the build.</param>
        /// <param name="buildConfig">The build configuration.</param>
        /// <returns>
        /// True if the build is successful
        /// </returns>
        public async Task<bool> Build(string projPath, string buildType = "rebuild", string buildConfig = "Debug")
        {
            logger.Write($"Building '{projPath}'");

           return await Task.Run(() =>
            {
                string devenv = Path.Combine(GetVisualStudioInstalledPath(), "devenv.com");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = devenv,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments =
                        $"\"{projPath}\" /{buildType} {buildConfig} /project \"{projPath}\" /projectconfig {buildConfig}"
                };

                bool result;

                using (Process process = Process.Start(startInfo))
                {
                    string outputLine;

                    while ((outputLine = process.StandardOutput.ReadLine()) != null)
                    {
                        logger.Write(outputLine);
                    }

                    process.WaitForExit();

                    result = process.ExitCode == 0;
                    return result;
                }
            });
        }

        /// <summary>
        /// Gets the visual studio installed path.
        /// </summary>
        internal string GetVisualStudioInstalledPath()
        {
            var visualStudioInstalledPath = string.Empty;
            var visualStudioRegistryPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0");
            if (visualStudioRegistryPath != null)
            {
                visualStudioInstalledPath = visualStudioRegistryPath.GetValue("InstallDir", string.Empty) as string;
            }

            if (string.IsNullOrEmpty(visualStudioInstalledPath) || !Directory.Exists(visualStudioInstalledPath))
            {
                visualStudioRegistryPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0");
                if (visualStudioRegistryPath != null)
                {
                    visualStudioInstalledPath = visualStudioRegistryPath.GetValue("InstallDir", string.Empty) as string;
                }
            }

            if (string.IsNullOrEmpty(visualStudioInstalledPath) || !Directory.Exists(visualStudioInstalledPath))
            {
                visualStudioRegistryPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\12.0");
                if (visualStudioRegistryPath != null)
                {
                    visualStudioInstalledPath = visualStudioRegistryPath.GetValue("InstallDir", string.Empty) as string;
                }
            }

            if (string.IsNullOrEmpty(visualStudioInstalledPath) || !Directory.Exists(visualStudioInstalledPath))
            {
                visualStudioRegistryPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\12.0");
                if (visualStudioRegistryPath != null)
                {
                    visualStudioInstalledPath = visualStudioRegistryPath.GetValue("InstallDir", string.Empty) as string;
                }
            }

            return visualStudioInstalledPath;
        }
    }
}
