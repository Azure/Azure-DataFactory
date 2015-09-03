This tool enables you to convert the Azure Data Factory JSON files to the latest format.

After you have installed the tool, you can find the JsonConversionTool.exe in 
C:\Program Files\Microsoft\Data Factory JSON Conversion Tool

For the JSON changes, see 
https://azure.microsoft.com/en-us/documentation/articles/data-factory-release-notes/#notes-for-07172015-release-of-data-factory

JsonConversionTool /sd <source directory> /td <target directory> [/v <target API version>]
Where:
/sd <source directory>  -  Source Directory containing JSON definitions.
/td <target directory>  -  Target Directory for new Upgraded JSON defintions. 
/v <target API version> -  This is an optional argument. 
                           By default latest API version: '2015-07-01-preview' will be assumed. 
                           Supported target API versions: 2014-12-01-preview, 2015-01-01-preview, 2015-05-01-preview, 2015-07-01-preview.
