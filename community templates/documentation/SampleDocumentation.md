# [Insert Template Title]

[INSERT template background & use cases] 

This article describes a solution template that [INSERT template description]. 


## About this solution template

This template [INSERT template actions (e.g. This template moves data from a SQL source to blob sink via Copy activity.)]

The template contains [INSERT Number] activities:
- **GetMetadata** gets the list of objects including the files and subfolders from your folder on source store. It will not retrieve the objects recursively. 
- **Filter** filter the objects list from **GetMetadata** activity to select the files only. 
- **ForEach** gets the file list from the **Filter** activity and then iterates over the list and passes each file to the Copy activity and Delete activity.
- **Copy** copies one file from the source to the destination store.
- **Delete** deletes the same one file from the source store.

The template defines [INSERT Number] parameters:
- *SourceStore_Location* is the folder path of your source store where you want to move files from. 
- *SourceStore_Directory* is the subfolder path of your source store where you want to move files from.
- *DestinationStore_Location* is the folder path of your destination store where you want to move files to. 
- *DestinationStore_Directory* is the subfolder path of your destination store where you want to move files to.

## How to use this solution template

1. Go to the **Move files** template. Select existing connection or create a **New** connection to your source file store where you want to move files from. Be aware that **DataSource_Folder** and **DataSource_File** are reference to the same connection of your source file store.
    
    :::image type="content" source="media/solution-template-move-files/move-files-1.png" alt-text="Screenshot showing creation of a new connection to the source." lightbox="media/solution-template-move-files/move-files-1.png" :::

2. Select existing connection or create a **New** connection to your destination file store where you want to move files to.

    :::image type="content" source="media/solution-template-move-files/move-files-2.png" alt-text="Screenshot showing creation a new connection to the destination." lightbox="media/solution-template-move-files/move-files-2.png" :::

3. Select **Use this template** tab.
	
4. You'll see the pipeline, as in the following example:

    :::image type="content" source="media/solution-template-move-files/move-files-4.png" alt-text="Screenshot showing the pipeline.":::

5. Select **Debug**, enter the **Parameters**, and then select **Finish**.   The parameters are the folder path where you want to move files from and the folder path where you want to move files to. 

    :::image type="content" source="media/solution-template-move-files/move-files5.png" alt-text="Screenshot showing where to run the pipeline.":::

6. Review the result.

    :::image type="content" source="media/solution-template-move-files/move-files6.png" alt-text="Screenshot showing the result of the pipeline run.":::
