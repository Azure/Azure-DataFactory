using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CustomActivityRunner
{
    /// <summary>
    /// This attribute when applied to a custom DotNetActivity allows it to be debugged by clicking on the debug button that appears beside the RunActivity method.
    /// </summary>
    public class CustomActivityAttribute : TestAttribute
    {
        /// <summary>
        /// Specify the location of the pipeline json file relative to the project hosting the custom DotNetActivity.
        /// </summary>
        public string PipelineLocation { get; set; }

        /// <summary>
        /// The name of the activity to debug.
        /// </summary>
        public string ActivityName { get; set; }

        /// <summary>
        /// The name of the deployment configuration file you wish to use with the activity. This is optional.
        /// </summary>
        public string DeployConfig { get; set; }
    }
}
