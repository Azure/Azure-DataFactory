namespace AzureAnalysisServicesProcessSample
{
    using Microsoft.Azure.Management.DataFactories.Models;
    using Microsoft.Azure.Management.DataFactories.Runtime;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public abstract class CrossAppDomainDotNetActivity<TExecutionContext>
        : MarshalByRefObject, IActivityLogger, ICrossAppDomainDotNetActivity<TExecutionContext>, IDotNetActivity
        where TExecutionContext : class
    {
        IActivityLogger logger;

        IDictionary<string, string> IDotNetActivity.Execute(IEnumerable<LinkedService> linkedServices,
            IEnumerable<Dataset> datasets, Activity activity, IActivityLogger logger)
        {
            TExecutionContext context = this.PreExecute(linkedServices, datasets, activity, logger);

            Type myType = this.GetType();
            var assemblyLocation = new FileInfo(myType.Assembly.Location);
            var appDomainSetup = new AppDomainSetup
            {
                ApplicationBase = assemblyLocation.DirectoryName,
                ConfigurationFile = assemblyLocation.Name + ".config"
            };
            AppDomain appDomain = AppDomain.CreateDomain(myType.ToString(), null, appDomainSetup);
            var proxy = (ICrossAppDomainDotNetActivity<TExecutionContext>)
                appDomain.CreateInstanceAndUnwrap(myType.Assembly.FullName, myType.FullName);
            this.logger = logger;
            return proxy.Execute(context, (IActivityLogger)this);
        }

        public abstract IDictionary<string, string> Execute(TExecutionContext context, IActivityLogger logger);

        public override object InitializeLifetimeService()
        {
            // Ensure that the client-activated object lives as long as the hosting app domain.
            return null;
        }

        internal virtual TExecutionContext PreExecute(IEnumerable<LinkedService> linkedServices,
            IEnumerable<Dataset> datasets, Activity activity, IActivityLogger logger)
        {
            return null;
        }

        void IActivityLogger.Write(string format, params object[] args)
        {
            this.logger.Write(format, args);
        }
    }
}
