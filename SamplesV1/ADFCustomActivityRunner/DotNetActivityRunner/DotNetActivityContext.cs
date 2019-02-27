using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.Azure.Management.DataFactories.Runtime;

namespace Microsoft.ADF.DotNetActivityRunner
{
    public class DotNetActivityContext
    {
        public List<LinkedService> LinkedServices { get; set; }
        public List<Dataset> Datasets { get; set; }
        public Activity Activity { get; set; }
        public IActivityLogger Logger { get; set; }
    }
}
