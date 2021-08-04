## Change Data Capture

### Step 1 Create Control Table and Stored Procedure used by Azure Data Factory

Use the following SQL script [ControlTableForSourceToSink.sql](https://github.com/DataSnowman/ChangeDataCapture/blob/main/usecases/cdc/code/sqlscripts/ControlTableForSourceToSink.sql) to create the ControlTableForSourceToSink table in the database deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator.

![Step 1 table](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/cdcstep1table.png)

Use the following SQL script [spUpdateWatermark.sql](https://github.com/DataSnowman/ChangeDataCapture/blob/main/usecases/cdc/code/sqlscripts/spUpdateWatermark.sql) to create the spUpdateWatermark stored procedure in the database deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator.

![Step 1 sproc](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/cdcstep1sproc.png)

Use the following SQL script [CreateStudent.sql](https://github.com/DataSnowman/ChangeDataCapture/blob/main/usecases/cdc/code/sqlscripts/CreateStudent.sql) to create the studentMath table in the database deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator.

![Step 1 student](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/cdcstep1student.png)

### Step 2 Create Azure Data Factory Pipeline from local Template

Download [ADF Template zip](https://github.com/DataSnowman/ChangeDataCapture/tree/main/usecases/cdc/code/adfTemplates)

![adftemplatezip](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adftemplatezip.png)

Open up the ADF deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator.  Select Pipeline from template 

![adfplfromtemplate](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfplfromtemplate.png)

Select Use local template

![adfUseLocalTemplate](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfUseLocalTemplate.png)

Open local template

![adfOpenLocalTemplate](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfOpenLocalTemplate.png)

It should look like this

![adftemplateUserinputs](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adftemplateUserinputs.png)

Select +New in the first User input and create an Azure SQL Database Linked Service to the database deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator

![adfDatabaseLinkedService](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfDatabaseLinkedService.png)

Select +New in the second User input and create an Azure Data Lake Storage Gen2 Linked Service 

![adfAdlsLinkedService](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfAdlsLinkedService.png)

For Input 3 select the same Database you chose in Input 1 

For Input 4 select the same Database you chose in Input 1

For Input 5 select the same Storage input you chose in Input 2

For Input 6 select the same Database you chose in Input 1 

For Input 7 select the same Database you chose in Input 1

Then click on Use this template

![adfAllUserinputs.png](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfAllUserinputs.png)

It should look like this when it is imported

![adfTemplateImported](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfTemplateImported.png)

### Step 3 Debug the DeltaCopyAndFullCopyfromDB_using_ControlTable Pipeline 

Click on Debug, enter the name of the Control table `ControlTableForSourceToSink`
Click OK

![adfDebugPipelineRun](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfDebugPipelineRun.png)

Once the pipeline runs successfully it should look like this

![adfSuccessfulRun](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfSuccessfulRun.png)

Check that the files have been created in Storage using Azure Storage Explorer of Azure Portal in the browser.  The files should be in bronze container at a path like `CDC/Sales/Microsoft/AdventureWorksLT/SalesLT/Address/`

![adfFileInStorage](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfFileInStorage.png)

If you run the pipeline a second time you will see another file created.  Since the Address table has a ModifiedDate column which can be used as a Watermark the second file (smaller 102 bytes) only contains a header since there were no changes (unless some updates are done to the Address table)

![adfFileInStorage2](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfFileInStorage2.png)

If you compare this file to the studentMath files (which does not have a watermark column) they are the same size because it in not doing a delta.  The file will get larger as inserts and update happen in the studentMath table.

![adfFileInStorage3](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfFileInStorage3.png)


You can now save the pipline by clickin on Publish all

![adfPublishAll](https://raw.githubusercontent.com/DataSnowman/ChangeDataCapture/main/images/adfPublishAll.png)

### Step 4 Import, configure, and run the Databrick Notebook

TBD