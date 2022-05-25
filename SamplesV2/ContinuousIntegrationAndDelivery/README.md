
### Sample pre- and post-deployment script (PrePostDeploymentScript.ps1)

This sample script can be used to stop triggers before deployment and restart them afterward. The script also includes code to delete resources that have been removed

* When running a pre-deployment script, please specify a variation of the following parameters in the Script Arguments field.

```azurepowershell
-armTemplate "$(System.DefaultWorkingDirectory)/<your-arm-template-location>" -ResourceGroupName <your-resource-group-name> -DataFactoryName <your-data-factory-name> -predeployment $true -deleteDeployment $false
```

* When running a post-deployment script, please specify a variation of the following parameters in the Script Arguments field.

```azurepowershell
-armTemplate "$(System.DefaultWorkingDirectory)/<your-arm-template-location>" -ResourceGroupName <your-resource-group-name> -DataFactoryName <your-data-factory-name> -predeployment $false -deleteDeployment $true
```

### Sample script to deploy global parameters (GlobalParametersUpdateScript.ps1)

This script can be used to promote global parameters to additional environments
