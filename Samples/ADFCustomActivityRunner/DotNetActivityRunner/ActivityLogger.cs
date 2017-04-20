using Microsoft.Azure.Management.DataFactories.Runtime;
using System;
using System.Diagnostics;

namespace Microsoft.ADF.DotNetActivityRunner
{
    internal class ActivityLogger : IActivityLogger
    {
        internal ActivityLogger()
        {
            Trace.AutoFlush = true;
        }

        public void Write(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Trace.WriteLine(string.Format(format, args));
        }
    }
}