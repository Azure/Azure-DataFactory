# Sample of app-domain isolation for .NET activity

This sample allows you to author a custom .NET activity for ADF that is not constrained to assembly versions used by the ADF launcher (e.g., WindowsAzure.Storage v4.3.0, Newtonsoft.Json v6.0.x, etc.).

The code includes an abstract base class (`CrossAppDomainDotNetActivity`) that implements app-domain isolation and a sample derived class (`MyDotNetActivity`) that demonstrates using WindowsAzure.Storage v6.2.0.

Note: The public types exposed by the ADF SDK are not serializable across app domain boundaries. As such, the derived class must provide pre-execution logic (`PreExecute`) to process the ADF objects into a serializable object that is then passed to the core logic (`Execute`).

## Detailed steps

Follow these steps in order to apply the sample to your project:

1. Create a `[Serializable]` context class (e.g., `MyDotNetActivityContext`) to hold the info your activity logic needs from the linked services, datasets, and/or activity.

2. Create a subclass of `CrossAppDomainDotNetActivity<MyDotNetActivityContext>` (e.g., `MyDotNetActivity`).

3. Override `PreExecute()` to pull necessary info from the linked services, datasets, and/or activities and create a new instance of `MyDotNetActivityContext`.

4. Override `Execute()` to implement your activity logic based on the context object.  This executes in a separate app domain and can reference any assembly versions.

See `MyDotNetActivity` & `MyDotNetActivityContext` as an example of doing this.
