using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ADF.Deployment.AdfKeyVaultDeployment
{
    public class Logger : ILogger
    {
        public Logger()
        {
            Trace.AutoFlush = true;
        }

        public void Write(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Trace.WriteLine(string.Format(format, args) + "\r\n");
        }

        public void WriteError(Exception e)
        {
            Console.WriteLine(e);
            Trace.WriteLine(e + "\r\n");
        }
    }
}
