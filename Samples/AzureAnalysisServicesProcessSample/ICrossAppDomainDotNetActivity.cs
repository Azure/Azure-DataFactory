namespace AzureAnalysisServicesProcessSample
{
    using Microsoft.Azure.Management.DataFactories.Runtime;
    using System.Collections.Generic;

    interface ICrossAppDomainDotNetActivity<TExecutionContext>
    {
        IDictionary<string, string> Execute(TExecutionContext context, IActivityLogger logger);
    }
}
