# Community pipeline templates #

We are excited to announce that Community Templates are now available to our community members. For more information about this announcement, please see
[Introducing Azure Data Factory Community Templates](https://techcommunity.microsoft.com/t5/azure-data-factory-blog/introducing-azure-data-factory-community-templates/ba-p/3650989). 

Pipeline templates should follow ARM templates syntax. For more information about ARM templates, see [https://go.microsoft.com/fwlink/?linkid=2181481](https://go.microsoft.com/fwlink/?linkid=2181481)

Notes for "manifest.json":
1. It must contain a property named "contributorType" set to "Community".
2. The "author" property can be configured as the contributor of the template.
3. The "documentation" property must be set to a link. Please upload your documentation to the documentation folder in community templates. 

# Create templates from pipelines #

In addition to writing a template file directly, you can also create a template from a a new or existing pipeline in [Azure Data Factory](https://adf.azure.com). 

For details about pipelines, please refer [Data Integration Pipelines](https://go.microsoft.com/fwlink/?linkid=2181927).  

To create a template from a pipeline, please go to [Azure Data Factory](https://adf.azure.com) and execute the following steps:

1. Navigate to your data factroy, in the left side menu choose "Author" to open the authoring blade.

   ![Open authoring pane](images/open-authoring-pane01.png?raw=true)

2. In the authoring blade, click the plus symbol and then click "Pipeline" to open the submenu.

   ![Open pipeline menu](images/open-authoring-pane03.png?raw=true)

3. Click on the 'Pipeline' button in the submenu to open the pipeline configuration pane.

   ![Create a pipeline](images/create-pipeline.png?raw=true)

4. After your pipeline is ready, click on the "Export template" button in the right side of the authoring blade to export a template.

    ![Export your template](images/export-template.png?raw=true)

5. At last, in the template file obtained in the previous steps, you need to add "contributorType" and "author" in "manifest.json" and you can add new properties or modify it as well.

# Test your templates #

To make sure the template is correct, please go to [Azure Data Factory](https://adf.azure.com) and import the template file to see if a pipeline can be created from it correctly or not.

To do so, execute the following steps: 

1. Navigate to your data factroy, in the left side menu choose "Author" to open the authoring blade.

   ![Open authoring pane](images/open-authoring-pane01.png?raw=true)

2. In the authoring blade, click the plus symbol and then click "Pipeline" to open the submenu.

   ![Open pipeline menu](images/open-authoring-pane02.png?raw=true)

3. Click on the 'Import from pipeline template' button in the submenu. Then select the local template file you want to check in the file selection dialog box. The template file should be in zip format.

   ![Import your template](images/import-local-templates.png?raw=true)


4. Once the template file is imported successfully, a pane will be triggered. If the pane contains one or more input elements for linked services which are configured in the template file, select a linked service for each input.

   ![Configure your template](images/use-template01.png?raw=true)

   If the template is incorrect, an error hint will show in the top of the pane and "Use this template" button will be disabled.

   ![Invalid template](images/invalid-template.png?raw=true)

5. When the "Use this template" button is enabled, click on it and navigate back to the authoring blade.

   ![Use your template](images/use-template02.png?raw=true)


6. If the template is correct, a pipeline will be created from it automatically.

   ![Create a pipeline](images/pipeline-from-template.png?raw=true)

# Submit your templates #

  You're very welcomed to provide your templates to [Azure-DataFactory](https://github.com/Azure/Azure-DataFactory)！

Put your templates in the "community templates" folder and create a pull request. We will review your templates. Once they are verified to be correct, we will deploy them to the Azure Data Factory template gallery. 
