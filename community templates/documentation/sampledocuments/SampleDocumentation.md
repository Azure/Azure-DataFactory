# [Insert Template Title]

For more documentation references, please visit the ADF docs page: [Template Documentation Example](https://user-images.githubusercontent.com/76960847/195502859-13bea2bd-f9bf-44a3-a828-112fe32d2d07.png)


[INSERT template background & use cases] 

This article describes a solution template that [INSERT template description]. 


## About this solution template

This template [INSERT template actions (e.g. This template moves data from a SQL source to blob sink via Copy activity.)]

_Example:_

The template contains [INSERT Number] activities:
- **GetMetadata** gets the list of objects including the files and subfolders from your folder on source store. It will not retrieve the objects recursively. 
- **Filter** filter the objects list from **GetMetadata** activity to select the files only. 
- **ForEach** gets the file list from the **Filter** activity and then iterates over the list and passes each file to the Copy activity and Delete activity.
- **Copy** copies one file from the source to the destination store.
- **Delete** deletes the same one file from the source store.

_Example:_

The template defines [INSERT Number] parameters:
- *SourceStore_Location* is the folder path of your source store where you want to move files from. 
- *SourceStore_Directory* is the subfolder path of your source store where you want to move files from.
- *DestinationStore_Location* is the folder path of your destination store where you want to move files to. 
- *DestinationStore_Directory* is the subfolder path of your destination store where you want to move files to.

## How to use this solution template

_Example instructions:_

1. Go to the **Move files** template. Select existing connection or create a **New** connection to your source file store where you want to move files from. Be aware that **DataSource_Folder** and **DataSource_File** are reference to the same connection of your source file store.
    
    [INSERT SCREENSHOT]

2. Select existing connection or create a **New** connection to your destination file store where you want to move files to.

    [INSERT SCREENSHOT]
    
3. Select **Use this template** tab.
	
4. You'll see the pipeline, as in the following example:

    [INSERT SCREENSHOT]

5. Select **Debug**, enter the **Parameters**, and then select **Finish**.   The parameters are the folder path where you want to move files from and the folder path where you want to move files to. 

    [INSERT SCREENSHOT]

6. Review the result.

    [INSERT SCREENSHOT]


