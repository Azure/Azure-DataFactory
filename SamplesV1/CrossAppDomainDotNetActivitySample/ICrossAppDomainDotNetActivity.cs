// Copyright (c) Microsoft Corporation. All Rights Reserved.

using Microsoft.Azure.Management.DataFactories.Runtime;
using System.Collections.Generic;

namespace CrossAppDomainDotNetActivitySample
{
    interface ICrossAppDomainDotNetActivity<TExecutionContext>
    {
        IDictionary<string, string> Execute(TExecutionContext context, IActivityLogger logger);
    }
}
