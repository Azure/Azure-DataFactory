# Azure Data Factory Samples #

The samples for the Azure Data Factory follow ARM templates standard. For more information about ARM templates, see [https://go.microsoft.com/fwlink/?linkid=2181481](https://go.microsoft.com/fwlink/?linkid=2181481)

manifest.json must contain a property named "contributorType" set to "Community". Meanwhile, "author" property can be configured as the contributor of the sample.

The sample file name should be unique both in this folder and templates folder.

An example is "A Community Template Sample" in this folder.

# Test samples #

To make sure the sample is correct, please go to Azure Data Factory and import the sample file to see if a pipeline can be created from it correctly.

To do so, execute the following steps: 

1. Navigate to your data factroy, in the left side menu choose "Author" to open the authoring blade.

![Open authoring pane](images/open-authoring-pane01.png?raw=true)

2. In the authoring blade, click the plus symbol and then click "Pipeline" to open the submenu.

![Open pipeline menu](images/open-authoring-pane02.png?raw=true)

3. Click on the 'Import from pipeline template' button in the submenu. Then select the local sample file you want to check in the file selection dialog box. The sample file should be in zip format.

![Import your template](images/import-local-templates.png?raw=true)


4. Once the sample file is imported successfully, a pane with one or more input elements for linked services which are configured in the sample file will be triggered. Select a linked service for each input.

![Configure your template](images/use-template01.png?raw=true)

If the template is incorrect, an error hint will show in the top of pane and the "Use this template" button will be disabled.

![Invalid template](images/invalid-template.png?raw=true)

5. When the "Use this template" button is enabled, click on it and navigate back to authoring blade.

![Use your template](images/use-template02.png?raw=true)


6. If the sample file is correct, a pipeline will be created from it automatically.

![Create a pipeline](images/pipeline-from-template.png?raw=true)