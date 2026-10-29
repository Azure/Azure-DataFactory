# Contribute Code or Provide Feedback #

## Contributing to the Azure Data Factory samples

### Getting Started
All users must sign the [Microsoft Contributor License Agreement (CLA)](https://cla.opensource.microsoft.com/) before any code contribution can be made. 

For Microsoft employees, make sure that your GitHub account is part of the Azure organization. [Use this page](http://aka.ms/azuregithub) to link your account.

### Template validation
Pull requests targeting `main`, including pull requests from forks, run the Template Validation workflow against the PR's base commit on `main`, not the proposed PR changes, preserving the original validation scope. The workflow uses a read-only token, does not persist checkout credentials, and reports validation details in the Actions run summary instead of posting PR comments. Validation errors and warnings do not fail the check; workflow execution failures still do. Manual runs remain available through `workflow_dispatch` and validate the selected ref.

### Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). 

For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
