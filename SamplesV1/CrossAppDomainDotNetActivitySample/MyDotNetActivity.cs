// Copyright (c) Microsoft Corporation. All Rights Reserved.

using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.Azure.Management.DataFactories.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossAppDomainDotNetActivitySample
{
    // NOTE: This is a *toy* implementation of CrossAppDomainDotNetActivity.  Proper error handling has been elided 
    // for brevity's sake.  A production implementation should include proper error handling.
    class MyDotNetActivity : CrossAppDomainDotNetActivity<MyDotNetActivityContext>
    {
        protected override MyDotNetActivityContext PreExecute(IEnumerable<LinkedService> linkedServices,
            IEnumerable<Dataset> datasets, Activity activity, IActivityLogger logger)
        {
            WriteGreeting(logger);
            // Process ADF artifacts up front as these objects are not serializable across app domain boundaries.
            Dataset dataset = datasets.First(ds => ds.Name == activity.Inputs.Single().Name);
            var blobProperties = (AzureBlobDataset)dataset.Properties.TypeProperties;
            LinkedService linkedService = linkedServices.First(ls => ls.Name == dataset.Properties.LinkedServiceName);
            var storageProperties = (AzureStorageLinkedService)linkedService.Properties.TypeProperties;
            return new MyDotNetActivityContext
            {
                ConnectionString = storageProperties.ConnectionString,
                FolderPath = blobProperties.FolderPath,
                FileName = blobProperties.FileName
            };
        }

        public override IDictionary<string, string> Execute(MyDotNetActivityContext context, IActivityLogger logger)
        {
            WriteGreeting(logger);
            // This demonstrates using a type (i.e., CloudBlob) available in Azure storage 6.2 but not 4.3.
            CloudStorageAccount account = CloudStorageAccount.Parse(context.ConnectionString);
            var blob = new CloudBlob(
                new Uri(new Uri(account.BlobEndpoint, context.FolderPath), context.FileName), account.Credentials);
            string message = string.Format("The blob's type is '{0}' and it does{1} exist.",
                blob.BlobType, blob.Exists() ? "" : "n't");
            logger.Write(message);
            return new Dictionary<string, string>() { { "Message", message } };
        }

        static void WriteGreeting(IActivityLogger logger)
        {
            // This demonstrates in which app domain the caller is running.
            logger.Write("Hello, world, from app domain '{0}'!", AppDomain.CurrentDomain.FriendlyName);
        }
    }
}
