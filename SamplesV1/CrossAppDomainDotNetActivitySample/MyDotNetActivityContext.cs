// Copyright (c) Microsoft Corporation. All Rights Reserved.

using System;

namespace CrossAppDomainDotNetActivitySample
{
    [Serializable]
    class MyDotNetActivityContext
    {
        public string ConnectionString { get; set; }
        public string FolderPath { get; set; }
        public string FileName { get; set; }
    }
}
