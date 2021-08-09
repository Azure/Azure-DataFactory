## Change Data Capture

### Step 1 Create Control Table and Stored Procedure used by Azure Data Factory

Use the following SQL script [ControlTableForSourceToSink.sql](https://github.com/DataSnowman/analytics-accelerator/blob/main/usecases/cdc/code/sqlscripts/ControlTableForSourceToSink.sql) to create the ControlTableForSourceToSink table in the database deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator.

![Step 1 table](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/cdcstep1table.png)

Use the following SQL script [spUpdateWatermark.sql](https://github.com/DataSnowman/analytics-accelerator/blob/main/usecases/cdc/code/sqlscripts/spUpdateWatermark.sql) to create the spUpdateWatermark stored procedure in the database deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator.

![Step 1 sproc](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/cdcstep1sproc.png)

Use the following SQL script [CreateStudent.sql](https://github.com/DataSnowman/analytics-accelerator/blob/main/usecases/cdc/code/sqlscripts/CreateStudent.sql) to create the studentMath table in the database deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator.

![Step 1 student](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/cdcstep1student.png)

### Step 2 Create Azure Data Factory Pipeline from local Template

Download [ADF Template zip](https://github.com/DataSnowman/analytics-accelerator/tree/main/usecases/cdc/code/adfTemplates)

![adftemplatezip](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adftemplatezip.png)

Open up the ADF deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator.  Select Pipeline from template 

![adfplfromtemplate](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfplfromtemplate.png)

Select Use local template

![adfUseLocalTemplate](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfUseLocalTemplate.png)

Open local template

![adfOpenLocalTemplate](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfOpenLocalTemplate.png)

It should look like this

![adftemplateUserinputs](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adftemplateUserinputs.png)

Select +New in the first User input and create an Azure SQL Database Linked Service to the database deployed by the ARM template in the Deploy an Azure Databricks Workspace Azure Analytics Accelerator

![adfDatabaseLinkedService](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfDatabaseLinkedService.png)

Select +New in the second User input and create an Azure Data Lake Storage Gen2 Linked Service 

![adfAdlsLinkedService](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfAdlsLinkedService.png)

For Input 3 select the same Database you chose in Input 1 

For Input 4 select the same Database you chose in Input 1

For Input 5 select the same Storage input you chose in Input 2

For Input 6 select the same Database you chose in Input 1 

For Input 7 select the same Database you chose in Input 1

Then click on Use this template

![adfAllUserinputs.png](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfAllUserinputs.png)

It should look like this when it is imported

![adfTemplateImported](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfTemplateImported.png)

### Step 3 Debug the DeltaCopyAndFullCopyfromDB_using_ControlTable Pipeline 

Click on Debug, enter the name of the Control table `ControlTableForSourceToSink`
Click OK

![adfDebugPipelineRun](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfDebugPipelineRun.png)

Once the pipeline runs successfully it should look like this

![adfSuccessfulRun](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfSuccessfulRun.png)

Check that the files have been created in Storage using Azure Storage Explorer of Azure Portal in the browser.  The files should be in bronze container at a path like `CDC/Sales/Microsoft/AdventureWorksLT/SalesLT/Address/`

![adfFileInStorage](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfFileInStorage.png)

If you run the pipeline a second time you will see another file created.  Since the Address table has a ModifiedDate column which can be used as a Watermark the second file (smaller 102 bytes) only contains a header since there were no changes (unless some updates are done to the Address table)

![adfFileInStorage2](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfFileInStorage2.png)

If you compare this file to the studentMath files (which does not have a watermark column) they are the same size because it in not doing a delta.  The file will get larger as inserts and update happen in the studentMath table.

![adfFileInStorage3](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfFileInStorage3.png)


You can now save the pipline by clickin on Publish all

![adfPublishAll](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfPublishAll.png)

### Step 4 Import, configure, and run the Databrick notebook

#### Requirements

- Databricks Runtime 8.3 or above when you create your cluster

- Setup Permissions to ADLS Gen2

- Secrets in Key vault

*Steps*

#### Import the Databricks notebook

Open up you Databricks workspace and navigate to your user, select the dropdown and select import

![adbworkspace](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbworkspace.png)

Import from file if you cloned the repo locally or enter the URL to the Notebook in GitHub Repo [autoloadersp.ipynb](https://github.com/DataSnowman/analytics-accelerator/blob/main/usecases/cdc/code/notebooks/autoloadersp.ipynb) and click Import

![adbnotebookimport](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbnotebookimport.png)

You should now have a notebook that looks like this:

![adbnotebook](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbnotebook.png)

Change the value of the adlsAccountName = "dataccelerr267cb5wtgfxg" in cell one to the adlsAccountName of in your deployment

In my chase my deployment has a Storage account name of `adfacceler7kdgtkhj5mpoa`

![adbrgservices](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbrgservices.png)

Change the values for `sourceAdlsFolderName` and `sinkAdlsFolderName` to `CDC/Sales/Microsoft/AdventureWorksLT/SalesLT/Address` to match the value in the columns in the `ControlTableForSourceToSink` table.  Note if you change any column values in the `ControlTableForSourceToSink` table make the appropriate changes.

![adbfolderpath](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbfolderpath.png)

The notebook would now look like this:

![adbadlsacctname](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbadlsacctname.png)

#### Configure Service Principal and Permissions

*Create a Service principal* [Reference](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#register-an-application-with-azure-ad-and-create-a-service-principal)

Create an [Azure Active Directory app and service principal](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal) in the form of client ID and client secret.

1. Sign in to your Azure Account through the Azure portal.

2. Select Azure Active Directory.

3. Select App registrations.

![adbappreg](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbappreg.png)

4. Select New registration.

Name the application something like `autoloader-darsch`. Select a supported account type, which determines who can use the application. After setting the values, select Register.

Note that it is a good idea to name the application with something unique to you like your email alias (darsch in my case) because other might use similar names like autoloader.

![adbregister](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbregister.png)

5. Copy the Directory (tenant) ID and store it to use to create an application secret.

6. Copy the Application (clinet) ID and store it to use to create an application secret.

![adbappids](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbappids.png)

*Assign Role Permissions* [Reference](https://docs.microsoft.com/en-us/azure/databricks/spark/latest/structured-streaming/auto-loader-gen2#permissions)

7. At storage account level assign this app the following roles to the storage account in which the input path resides:

    `Contributor`: This role is for setting up resources in your storage account, such as queues and event subscriptions.
    `Storage Queue Data Contributor`: This role is for performing queue operations such as retrieving and deleting messages from the queues. This role is required in Databricks Runtime 8.1 and above only when you provide a service principal without a connection string.|
    `Storage Blob Data Contributor` to access storage

![adbstorageiam](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbstorageiam.png)

8. At resource group level assign this app the following role to the related resource group:

    `EventGrid EventSubscription Contributor`: This role is for performing event grid subscription operations such as creating or listing event subscriptions.

![adbrgiam](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbrgiam.png)

*Create a new application secret*

- Select Azure Active Directory.

- From App registrations in Azure AD, select your application.

- Select Certificates & secrets.

- Select Client secrets -> New client secret.

- Provide a description of the secret, and a duration. When done, select Add.

![adbappsecret](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbappsecret.png)

After saving the client secret, the value of the client secret is displayed. Copy this value because you won't be able to retrieve the key later. You will provide the key value with the application ID to sign in as the application. Store the key value where your application can retrieve it.

![adbappsecretval](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbappsecretval.png)

#### Deploy a Key Vault and setup secrets

Create a Key Vault in the Resource group by clicking Create

![adbrgservices](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbrgservices.png)

Search for `Key vault`

![adbkvsearch](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbkvsearch.png)

Click Create

![adbkvcreate](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbkvcreate.png)

Create the Key Vault in the same Resource group and Region as you other resource deployed. Click Review and Create and then click Create

![adbrevcreate](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbrevcreate.png)

You should now have a Key vault in your resources

![adbrgwithkv](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbrgwithkv.png)

Open up you Key vault and add the appsecret created above

Choose Secrets and click Generate/Import

![adbkvsecretgen](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbkvsecretgen.png)

Enter you secret Name and paste in the app secret you created earlier, set activation date and click Create

![adbcreatesecret](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbcreatesecret.png)

It should look like this:

![adbfirstsecret](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbfirstsecret.png)

*Create the rest of the secrets you need for the notebook*

Create the rest of the secrets in cell 4 of the notebook

```
SubscriptionID = dbutils.secrets.get("c-change-autoloader","SubscriptionID")
DirectoryID = dbutils.secrets.get("c-change-autoloader","DirectoryID")
ServicePrincipalAppID = dbutils.secrets.get("c-change-autoloader","ServicePrincipalAppID")
ServicePrincipalSecret = dbutils.secrets.get("c-change-autoloader","appsecret")
ResourceGroup = dbutils.secrets.get("c-change-autoloader","ResourceGroup")
BlobConnectionKey = dbutils.secrets.get("c-change-autoloader","adls2-secret")
```
The adls2-secrect is created using the storage key

**Create an Azure Key Vault-backed secret scope using the UI** [Reference](https://docs.microsoft.com/en-us/azure/databricks/security/secrets/secret-scopes#create-an-azure-key-vault-backed-secret-scope-using-the-ui)

Verify that you have Contributor permission on the Azure Key Vault instance that you want to use to back the secret scope.

Go to https://<databricks-instance>#secrets/createScope. This URL is case sensitive; scope in createScope must be uppercase.

https://<databricks-instance>#secrets/createScope

In my case `https://adb-3272096941209353.13.azuredatabricks.net#secrets/createScope`

You can find the databricks-instance in the URL of your workspace

![adbinstance](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbinstance.png)

Enter Scope Name: I choose something like `c-change-autoloader` which is what I used in the notebook

Manage Principal:  `All Users`

DNS Name: `https://xxxxxx.vault.azure.net/` Find in the properites of Key vault under Vault URI

Resource ID: Find in the properties of the Key vault.  Looks something like this: 

```
/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx/resourcegroups/databricks-rg/providers/Microsoft.KeyVault/vaults/databricksKV
```
![adbsecretResID](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbsecretResID.png)

Click Create

![adbsecretscope](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbsecretscope.png)


#### Create a Databricks Cluster and attach to notebook

Create a cluster using the Runtime 8.3 or above

Enter Cluster Name, Runtime Version, Set Terminate after, Min Workers, Max Workers and click Create Cluster

![adbcreatecluster](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbcreatecluster.png)

#### Run the notebook one cell at a time (at least the first time)

Once the cluster is started you will be able to run the code in the cells

Click on Run Cell

![adbcruncell](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbcruncell.png)

Do this for the next cell down etc.

You can skip cell 6 the first time because nothing has been mounted.  You may get an error like this:

![adbunmount](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbunmount.png)

Just move on to cell 7 to mount the Source and Sink file system

The first time to run cell 16 comment out the 2 lines it references by putting # at beginning of each line.

```
 #.foreachBatch(upsertToDelta) # Comment this out first time you run
 #.queryName("c-changeLoader-merge") # Comment this out first time you run
```

![adbcell16](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbcell16.png)

After you run this the first time uncomment the 2 lines because you will want the upsert to run

Also notice that running the notebook has created a `Event Grid System Topic` in the resources

![adbeventgrid](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbeventgrid.png)

When you run the last cell 19 you should see 3 records for Everett WA

![adbcell19](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbcell19.png)

#### Go make a change to the Address table in Azure SQL Database, Run ADF Pipeline, and rerun cell 16 and 19

In a SQL Editor like Azure Data Studio or the browser run the following SQL to insert a new row

```
INSERT INTO [SalesLT].[Address]
        ([AddressLine1], [AddressLine2], [City], [StateProvince] ,[CountryRegion], [PostalCode])
 		VALUES
		('138th Drive', NULL, 'Everett', 'WA', 'USA', '98208')
```

You can cut and paste or use the following SQL script [InsertAddress.sql](https://github.com/DataSnowman/analytics-accelerator/blob/main/usecases/cdc/code/sqlscripts/InsertAddress.sql) 

Rerun the ADF Pipeline

Click on Debug, enter the name of the Control table `ControlTableForSourceToSink` Click OK

![adfDebugPipelineRun](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adfDebugPipelineRun.png)

Rerun cell 16 and 19 in the `autoloadersp` notebook

Make sure rows 4 and 5 of the code are uncommented so that the upsertToDelta function runs

![adbcell16again](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbcell16again.png)

This time you should see that new record

![adbcell19again](https://raw.githubusercontent.com/DataSnowman/analytics-accelerator/main/images/adbcell19again.png)

