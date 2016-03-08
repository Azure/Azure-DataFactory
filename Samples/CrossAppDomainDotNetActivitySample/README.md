# Sample of app-domain isolation for .NET activity

This sample allows you to author a custom .NET activity for ADF that is not constrained to assembly versions used by the ADF launcher (e.g., WindowsAzure.Storage v4.3.0, Newtonsoft.Json v6.0.x, etc.).

The code includes an abstract base class (CrossAppDomainDotNetActivity) that implements app-domain isolation and a sample derived class (MyDotNetActivity) that demonstrates using WindowsAzure.Storage v6.2.0.

Note: The public types exposed by the ADF SDK are not serializable across app domain boundaries. As such, the derived class must provide pre-execution logic (PreExecute) to process the ADF objects into a serializable object that is then passed to the core logic (ExecuteCore).
