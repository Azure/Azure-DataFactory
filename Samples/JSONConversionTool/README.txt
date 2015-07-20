This tool enables you to convert the JSON files to the latest format.

JsonUpgradeTool.exe /sd <source directory> /td <target directory> [/v <target API version>]
Where:
/sd <source directory>  -  Source Directory containing JSON definitions.
/td <target directory>  -  Target Directory for new Upgraded JSON defintions. 
/v <target API version> -  This is an optional argument. 
                           By default latest API version: '2015-07-01-preview' will be assumed. 
                           Supported target API versions: 2014-12-01-preview, 2015-01-01-preview, 2015-05-01-preview, 2015-07-01-preview.
