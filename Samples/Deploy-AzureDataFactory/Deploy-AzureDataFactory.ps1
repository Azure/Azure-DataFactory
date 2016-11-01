<#

.SYNOPSIS

Deploys an Azure Data Factory (ADF) from the command line. 


.DESCRIPTION

The script only deploys linked services, datasets and pipelines for a data factory, no current suport for Gateways.	
For this script to run correctly, in the PowerShell context in which this is called, you should have already loged in
to your Azure account and selected the correct subscription, e.g.:
	Login-AzureRmAccount
	Get-AzureRmSubscription –SubscriptionName “YourSubscription” | Select-AzureRmSubscription

The script expects the ADF's components to have specific suffixes for each of the elements
(linked service, dataset, pipeline) in order for them to be found and deployed correctly. 
The suffixes are configurable through the *FilenameSuffix set of parameters.

Support for linked services connection strings overwriting is supported as well, 
as long as that is passed in through the DataFactoryConfigFile parameter.

.PARAMETER SetupMode 

The mode in which the script will run. Valid options are:
	CHECK                         # Checks Azure Data Factory existence
	INSTALL                       # Deploys the new Azure Data Factory
	UPGRADE                       # Upgrades the Azure Data Factory in-place
	UNINSTALL                     # Removes the Azure Data Factory

.PARAMETER ResourceGroupName

The resource group for the ADF.

.PARAMETER Name

The name of the ADF.

.PARAMETER Location

The Azure region/location where a new Azure Data Factory will be installed.
This can be one of: WestUS, NorthEurope (the only 2 regions currently supported for ADF).

.PARAMETER DataFactoryDirectory

The location of the JSON scripts for the Azure Data Factory.
All required linked services, datasets and pipelines should be located in this directory.

.PARAMETER DataFactoryConfigFile

The location to the config file to use for deployment.


.PARAMETER Silence

If this switch is used, no user-prompts will be required when droping a data factory.
Use this with caution!

.PARAMETER DefaultLinkedServiceFilenameSuffix

The default file name suffix for Linked Services files in the ADF folder.
Default value is: "_LinkedService.json" 
An expected valid linked service filename, based on the default value
would then be: AzureSqlDW_LinkedService.json

.PARAMETER DefaultDatasetFilenameSuffix

The default file name suffix for Dataset files in the ADF folder.
Default value is: "_Dataset.json" 
An expected valid dataset filename, based on the default value
would then be: FileTable_Dataset.json

.PARAMETER DefaultPipelineFilenameSuffix

The default file name suffix for Pipeline files in the ADF folder.
Default value is: "_Pipeline.json"
An expected valid pipeline filename, based on the default value
would then be: CustomerSalesAggregation_Pipeline.json


.EXAMPLE

Enable and output all info/warning/error messages to a Log file (use *>> to append to the file instead):
. Deploy-AzureDataFactory.ps1 -InformationAction Continue -Verbose *> NewLogFile.txt

.EXAMPLE

Check that an ADF with the specified name exists:
.\Deploy-AzureDataFactory.ps1 -SetupMode CHECK -ResourceGroupName MyResourceGroup -Name MyDataFactory -InformationAction Continue -Verbose

.EXAMPLE

Installs an ADF:
 .\Deploy-AzureDataFactory.ps1 -SetupMode INSTALL -ResourceGroupName MyResourceGroup -Name MyDataFactory -InformationAction Continue -Verbose -Location NorthEurope  -DataFactoryDirectory "Z:\MyDataFactoryDirectory\"

.EXAMPLE

Upgrades (replaces) an existing ADF:
 .\Deploy-AzureDataFactory.ps1 -SetupMode UPGRADE -ResourceGroupName MyResourceGroup -Name MyDataFactory -InformationAction Continue -Verbose -Location NorthEurope  -DataFactoryDirectory "Z:\MyDataFactoryDirectory\"

.EXAMPLE

Upgrades (replaces) an existing ADF and uses a user-specified configuration file:
 .\Deploy-AzureDataFactory.ps1 -SetupMode UPGRADE -ResourceGroupName MyResourceGroup -Name MyDataFactory -InformationAction Continue -Verbose -Location NorthEurope  -DataFactoryDirectory "Z:\MyDataFactoryDirectory\" -DataFactoryConfigFile "Z:\MyDataFactoryDirectory\MyConfigFile.json"

.EXAMPLE

Delete an existing ADF without prompting the user for verification:
.\Deploy-AzureDataFactory.ps1 -SetupMode UNINSTALL -ResourceGroupName MyResourceGroup -Name MyDataFactory -InformationAction Continue -Verbose -Location NorthEurope -Silence


.NOTES
	Author: George Arion
	Version: 1.0.0

Requires PowerShell V5! If not installed on your machine (check with: "$PSVersionTable.PSVersion
"), Get "Windows Management Framework 5.0" from: https://www.microsoft.com/en-us/download/details.aspx?id=50395
Also see: https://technet.microsoft.com/en-us/library/hh857339.aspx

Requires AzureRM powershell modules (a one time setup per machine):
	Install-Module AzureRM

#>

# Script uses functionality from PowerShell v5
#Requires -Version 5

# Enable AdvancedFunctions for the script, this enables goodies such as the following flags: -Verbose, -Debug, etc.
[CmdletBinding()]
# Script Parameters
param(
	[Parameter(Mandatory=$true)]
	[ValidateSet("CHECK", "INSTALL", "UPGRADE", "UNINSTALL")]
	[string]$SetupMode,
	[Parameter(Mandatory=$true)]
	[ValidateScript({ -not ([string]::IsNullOrWhiteSpace($_)) })]
	[string]$ResourceGroupName,
	[Parameter(Mandatory=$true)]
	[ValidateScript({ -not ([string]::IsNullOrWhiteSpace($_)) })]
	[string]$Name,
	[ValidateSet("WestUS", "NorthEurope")]
	[string]$Location,
	[ValidateScript({Test-Path $_ -PathType 'Container'})]
	[string]$DataFactoryDirectory,
	[ValidateScript({Test-Path $_ -PathType 'Leaf'})]
	[string]$DataFactoryConfigFile,
	[switch]$Silence,
	[string]$DefaultLinkedServiceFilenameSuffix = "_LinkedService.json",
	[string]$DefaultDatasetFilenameSuffix = "_Dataset.json",
	[string]$DefaultPipelineFilenameSuffix = "_Pipeline.json")

# Enable the strictest coding checks for the script (this needs to be after the Param section)
Set-Strictmode -Version Latest

# Common data types
Enum SetupMode # Setup modes
{
	CHECK               = 1          # Checks ADF existence
	INSTALL             = 2          # Deploys new ADF
	UPGRADE             = 3          # Upgrades ADF in-place
	UNINSTALL           = 4          # Removes ADF
}

# Functions
# Main logic function
function Main
{
	Write-LogInformation "Started function: $($MyInvocation.MyCommand)"
	
	Check-Environment

	switch($SetupMode)
	{
		# Check if the database exits, given the provided credentials
		{$_ -eq [SetupMode]::CHECK}
		{
			$adfDetails = Get-AzureDataFactory
			if($adfDetails)
			{
				Write-LogInformation "The Azure Data Factory exists!"
				Write-LogVerbose $adfDetails
			}
			else
			{
				Write-LogInformation "The Azure Data Factory does not exist!"
			}
			break
		}

		# Create the database from scratch and upgrade it to the latest schema/version
		{$_ -eq [SetupMode]::INSTALL}
		{
			if( -not ($Location))
			{
				Write-LogError "Azure region/location is required for installing an ADF !"
				break
			}

			if( -not ($DataFactoryDirectory) -or -not(Test-Path $DataFactoryDirectory -Type 'Container') )
			{
				Write-LogError "A valid directory path (passed in with -DataFactoryDirectory CustomPath) is required for installing an ADF !"
				break
			}

			if( Install-AzureDataFactory )
			{
				Write-LogInformation "The Azure Data Factory has been installed."
			}
			else
			{
				Write-LogError "Azure Data Factory installation has failed! Cleaning up any potential ADF remains..."
				Delete-AzureDataFactory
			}

			break
		}

		<# Upgrade database to the current version from the $DatabaseOldVersion specified in the command line.
		If no version was specified, the script will retrieve and use the latest version from the Database itself. #>
		{$_ -eq [SetupMode]::UPGRADE}
		{
			if( -not(Test-Path $DataFactoryDirectory -Type 'Container') )
			{
				Write-LogError "A valid directory path (passed in with -DataFactoryDirectory CustomPath) is required for upgrading an ADF !"
				break
			}

			if(Upgrade-AzureDataFactory)
			{
				Write-LogInformation "The Azure Data Factory has been upgraded."
			}
			else
			{
				Write-LogError "The Azure Data Factory upgrade has failed!"
			}

			break
		}		

		# Drops the database
		{$_ -eq [SetupMode]::UNINSTALL}
		{
			# Check to see if the operation should proceed unprompted first
			if( -not ($Silence.IsPresent))
			{
				Write-LogWarning @"
You are about to DELETE the Azure Data Factory: $Name ! 
This operation can cause data loss and it cannot be reversed.
Are you sure you want to continue?
"@

				$userChoice = Read-Host "REMOVE Azure Data Factory? [y/n]"
				while($userChoice -ne 'y')
				{
					if($userChoice -eq 'n')
					{
						Write-Information "$SetupMode : Azure Data Factory uninstall operation aborted by user."
						return
					}

					$userChoice = Read-Host "REMOVE Azure Data Factory? [y/n]"
				}
			}

			if( Delete-AzureDataFactory )
			{
				Write-LogInformation "The Azure Data Factory: $Name has been dropped succesfully"
			}
			else 
			{
				Write-LogError "Could not remove the Azure Data Factory: $Name !"
			}

			break
		}

		# Wrong mode passed in
		default { Write-LogError "$SetupMode : Invalid SetupMode provided: $SetupMode"; break }
	}

	Write-LogInformation "Finished function: $($MyInvocation.MyCommand)"
}


# Verifies if the ADF exists, returns the details of the data factory as a string if it exists, $false otherwise
function Get-AzureDataFactory
{
	param(
	# If this is used, errors are hidden/not displayed to the user
	[switch]$HideErrors)

	$errorBehaviour = @{}
	if( $HideErrors )
	{
		$errorBehaviour.ErrorAction = "SilentlyContinue"
	}

	$formattedResult = ""
	try
	{
		$adfGetResponse = Get-AzureRmDataFactory -ResourceGroupName $ResourceGroupName -Name $Name @errorBehaviour

		$formattedResult += ($adfGetResponse | Out-String)
		return $formattedResult 
	}
	catch {
		if( -not $HideErrors )
		{
			Write-LogError "Could not find the Azure Data Factory! Error: `n$_"
		}
		return $false
	}

}


<#	Installs the ADF using the specified command-line parameters
	and the JSON scripts provided.
#>
function Install-AzureDataFactory
{
	Write-LogInformation "Started function: $($MyInvocation.MyCommand)"

	if( Get-AzureDataFactory -HideErrors)
	{
		# Critical error: exit script here to prevent any potential damage to existing ADF instances
		$errorMessage = "Install-AzureDataFactory: The ADF: $Name already exists!"
		Write-LogError $errorMessage
		throw  $errorMessage
	}

	Write-LogInformation "Creating the ADF: $Name"

	if( -not(Create-AzureDataFactory) )
	{
		Write-LogError "Install-AzureDataFactory: Can not create the ADF: $Name !"
		return $false
	}

	if( -not(Upgrade-AzureDataFactory -SkipExistsAdfCheck) )
	{
		Write-LogError "Install-AzureDataFactory: The contents of the ADF: $Name could not be pushed to Azure!"
		return $false
	}

	Write-LogInformation "Finished function: $($MyInvocation.MyCommand)"
	return $true
}

# Creates the ADF, returns $true on success, $false otherwise
function Create-AzureDataFactory
{
	try 
	{
		Write-LogVerbose "Create-AzureDataFactory: Running the following command: `nNew-AzureRmDataFactory -ResourceGroupName $ResourceGroupName -Name $Name -Location $Location"
		New-AzureRmDataFactory -ResourceGroupName $ResourceGroupName -Name $Name -Location $Location
	}
	catch
	{
	    Write-LogError "Could not create the ADF! Error: `n$_"
		return $false
	}

	return $true
}

# Upgrades the ADF, returns $true on success, $false otherwise
function Upgrade-AzureDataFactory
{
	param(
	# If this is used, checking for an existing ADF first is skipped. Relevant when called from install mode
	[switch]$SkipExistsAdfCheck)

	Write-LogInformation "Started function: $($MyInvocation.MyCommand)"

	if( (-not $SkipExistsAdfCheck) -and (-not (Get-AzureDataFactory -HideErrors)) )
	{
		Write-LogError "Upgrade-AzureDataFactory: The ADF: $Name does not exist and can not be upgraded!"
		return $false
	}

	if( -not (Deploy-LinkedServices) )
	{
		Write-LogError "Upgrade-AzureDataFactory: Could not deploy linked services for ADF: $Name !"
		return $false
	}

	if( -not (Deploy-Datasets) )
	{
		Write-LogError "Upgrade-AzureDataFactory: Could not deploy datasets for ADF: $Name !"
		return $false
	}

	if( -not (Deploy-Pipelines) )
	{	
		Write-LogError "Upgrade-AzureDataFactory: Could not deploy pipelines for ADF: $Name !"
		return $false
	}

	Write-LogInformation "Finished function: $($MyInvocation.MyCommand)"
	return $true
}

# Deploys the linked services for the ADF
function Deploy-LinkedServices
{
	Write-LogInformation "Started function: $($MyInvocation.MyCommand)"

	$linkedServicesFiles = Get-LinkedServices

	foreach($linkedServiceFile in $linkedServicesFiles)
	{
		if( -not (Deploy-LinkedService -filePath $linkedServiceFile) )
		{
			Write-LogError "Deploy-LinkedServices: Could not deploy the linked service from the following path: $linkedServiceFile"
			return $false
		}
	}

	Write-LogInformation "Finished function: $($MyInvocation.MyCommand)"
	return $true
}


<# Gets the linked services to be uploaded to the ADF.
Note: If a config file was passed in to the script, the Linked Services
will be matched against the config file and modified accordingly. #>
function Get-LinkedServices
{
	$linkedServicesFiles = @()

	if( $DataFactoryConfigFile )
	{
		$linkedServicesFiles = @(Get-LinkedServicesWithConfigData)
	}
	else
	{
		$linkedServicesFiles = @(Get-ChildItem $DataFactoryDirectory | Where { $_.FullName.EndsWith($DefaultLinkedServiceFilenameSuffix) } | Select-Object -ExpandProperty FullName)
	}

	return $linkedServicesFiles
}

# Get paths to modified LinkedServices according to the config file used
function Get-LinkedServicesWithConfigData
{
	$linkedServicesFilesToProcess = @()

	# Get the config JSON into a hashtable
	$configTable = Get-HashtableFromJsonFile -filePath $DataFactoryConfigFile

	# Get all existing linked services files
	$linkedServicesFiles = @(Get-ChildItem $DataFactoryDirectory | Where { $_.FullName.EndsWith($DefaultLinkedServiceFilenameSuffix) } | Select-Object -ExpandProperty FullName)

	# Foreach linked service file, attempt to see if it needs replacing values from the config and use that version if so (or the original otherwise)
	foreach($linkedService in $linkedServicesFiles)
	{
		$linkedServicesFilesToProcess += (Get-LinkedServiceWithConfig -linkedServicefilePath $linkedService -configTable $configTable )
	}

	return $linkedServicesFilesToProcess
}

<# Attempts to match a linked service json file (pass in a file path to it) 
with the config JSON (passed it as a variable containing a PSCustomObject from ConvertFrom-Json)

If there's a match, it will create a modified version of the linked service 
file and return the path to it.
Otherwise, it will return the path to the original, unmodified file.
Note: if the JSON processing fails, this will throw.
Note2: this function is *magic* (dynamic json navigation), however, unfortunately it does not support json indexing / full JSONPath language :(, just "." address form
#>
function Get-LinkedServiceWithConfig
{
	param(
		[Parameter(Mandatory=$true)]
		[ValidateScript({Test-Path $_ -PathType 'Leaf'})]
		[string]$linkedServicefilePath,
		[Parameter(Mandatory=$true)]		
		[hashtable]$configTable
	)
	
	# Get the LinkedService JSON in a hashtable
	$linkedServiceTable = Get-HashtableFromJsonFile -filePath $linkedServicefilePath

	# Verify if the linked service has an entry in the config, return original linked service if not
	$configValueForLinkedService = Get-ConfigValueForLinkedService -linkedServiceTable $linkedServiceTable -configTable $configTable
	if( -not ($configValueForLinkedService))
	{
		return $linkedServicefilePath
	}

	# Get a version of the linked service with the replaced settings
	#$linkedServiceTable = Get-HashTableWithJsonSettings -linkedServiceTable $linkedServiceTable -configValueForLinkedService $configValueForLinkedService

	$modifiedLinkedServiceFileName = "Modified-$(split-path $linkedServicefilePath -Leaf)"

	return (Get-LinkedServicePathModifiedFromJsonSettings -linkedServiceTable $linkedServiceTable -configValueForLinkedService $configValueForLinkedService -modifiedFileName $modifiedLinkedServiceFileName)
}


# Get the (temp) path to the Linked Service that was modified according to the json settings
function Get-LinkedServicePathModifiedFromJsonSettings
{
		param(
		[Parameter(Mandatory=$true)]		
		[hashtable]$linkedServiceTable,
		[Parameter(Mandatory=$true)]		
		[object[]]$configValueForLinkedService,
		[Parameter(Mandatory=$true)]		
		[string]$modifiedFileName
	)

	$modifiedLinkedServiceTargetPath = Join-Path $env:temp $modifiedFileName

	$modifiedLinkedServiceTable = Get-HashTableWithJsonSettings -linkedServiceTable $linkedServiceTable -configValueForLinkedService $configValueForLinkedService
	$modifiedLinkedServiceJson = $modifiedLinkedServiceTable | ConvertTo-Json
	$modifiedLinkedServiceFilePath = $modifiedLinkedServiceJson | Out-File -Force $modifiedLinkedServiceTargetPath

	# Convert the path back to a string
	return (Convert-Path $modifiedLinkedServiceTargetPath)
}


# Returns a hashtable with the contents changed according to the JSON settings
function Get-HashTableWithJsonSettings
{
	param(
		[Parameter(Mandatory=$true)]		
		[hashtable]$linkedServiceTable,
		[Parameter(Mandatory=$true)]		
		[object[]]$configValueForLinkedService
	)

	# Get the JSONPath location of the item that needs replacing, trimming the "$." from the start. NOTE: only supports JSONPaths with dots ".", no numerical/indexing navigation
	$jsonPathArrayToValueToBeReplaced = @($configValueForLinkedService.Name.Substring(2).Split('.'))

	$linkedServiceElement = $linkedServiceTable

	# Navigate to the proper element in the $linkedServiceTable object that contains the target value (we're targetting the container here!)
	for($i = 0; $i -lt ($jsonPathArrayToValueToBeReplaced.Length-1); $i++ )
	{
		$linkedServiceElement = $linkedServiceElement.($jsonPathArrayToValueToBeReplaced[$i])
	}

	# Replace the target path with the value from the config
	$linkedServiceElement.($jsonPathArrayToValueToBeReplaced[$i]) = $configValueForLinkedService.Value
	
	return $linkedServiceTable
}

# Returns true if the linked service hashtable has an equivalent "Name" entry in the config hashtable
function Get-ConfigValueForLinkedService
{	
	param(
		[Parameter(Mandatory=$true)]		
		[hashtable]$linkedServiceTable,
		[Parameter(Mandatory=$true)]		
		[hashtable]$configTable
	)

	$linkedServiceFoundInConfig = $false

	# Check if this is the linked service that we're looking for to replace a config in
	foreach($item in $configTable.GetEnumerator())
	{
		if( $item.Name -eq $linkedServiceTable.'Name' )
		{
			return $item.Value
		}
	}

	return $false
}




# Get a hashtable from a JSON file
function Get-HashtableFromJsonFile
{
	param(
		[Parameter(Mandatory=$true)]
		[ValidateScript({Test-Path $_ -PathType 'Leaf'})]
		[string]$filePath)

	$jsonContents = Get-Content $filePath  -Raw  | ConvertFrom-Json
	return (Get-HashtableFromJson -jsonObject $jsonContents)
}

# Returns a hashtable from a JSON PSCustomObject
function Get-HashtableFromJson
{
	param(
	[Parameter(Mandatory=$true)]
	[object]$jsonObject
	)	

	$hashTable = @{}
	$jsonObject.psobject.Properties | Foreach { $hashTable[$_.Name] = $_.Value }

	return $hashTable
}

# Deploys the datasets for the ADF
function Deploy-Datasets
{
	Write-LogInformation "Started function: $($MyInvocation.MyCommand)"

	$datasets = @(Get-ChildItem $DataFactoryDirectory | Where { $_.FullName.EndsWith($DefaultDatasetFilenameSuffix) } | Select-Object -ExpandProperty FullName)

	foreach($dataset in $datasets)
	{
		if( -not (Deploy-Dataset -filePath $dataset) )
		{
			Write-LogError "Deploy-Datasets: Could not deploy the dataset from the following path: $dataset"
			return $false
		}
	}

	Write-LogInformation "Finished function: $($MyInvocation.MyCommand)"
	return $true
}


# Deploys the pipelines for the ADF
function Deploy-Pipelines
{
	Write-LogInformation "Started function: $($MyInvocation.MyCommand)"

	$pipelines = @(Get-ChildItem $DataFactoryDirectory | Where { $_.FullName.EndsWith($DefaultPipelineFilenameSuffix) } | Select-Object -ExpandProperty FullName)

	foreach($pipeline in $pipelines)
	{
		if( -not (Deploy-Pipeline -filePath $pipeline) )
		{
			Write-LogError "Deploy-Pipelines: Could not deploy the pipeline from the following path: $pipeline"
			return $false
		}
	}

	Write-LogInformation "Finished function: $($MyInvocation.MyCommand)"
	return $true
}

# Deploys an individual linked service for the ADF
function Deploy-LinkedService
{
		param(
		[Parameter(Mandatory=$true)]
		[ValidateScript({Test-Path $_ -PathType 'Leaf'})]
		[string]$filePath)

		$linkedServiceName = [io.path]::GetFileNameWithoutExtension($filePath)
		try
		{
			Write-LogVerbose "Deploy-LinkedService: Running the following command: `nNew-AzureRmDataFactoryLinkedService -ResourceGroupName $ResourceGroupName -DataFactoryName $Name -Name $linkedServiceName -File $filePath -Force -ErrorAction Stop"
			$result = New-AzureRmDataFactoryLinkedService -ResourceGroupName $ResourceGroupName -DataFactoryName $Name -File $filePath -Force -ErrorAction Stop
			Write-LogInformation "Deploy-LinkedService: correctly deployed the linked service $linkedServiceName from: $filePath"
			return $true
		}
		catch
		{
			Write-LogError "Deploy-LinkedService: Could not deploy the LinkedService from: $filePath ! Error: `n$_"
			return $false
		}
}

# Deploys an individual dataset for the ADF
function Deploy-Dataset
{
		param(
		[Parameter(Mandatory=$true)]
		[ValidateScript({Test-Path $_ -PathType 'Leaf'})]
		[string]$filePath)

		$datasetName = [io.path]::GetFileNameWithoutExtension($filePath)
		try
		{
			Write-LogVerbose "Deploy-Dataset: Running the following command: `nNew-AzureRmDataFactoryDataset -ResourceGroupName $ResourceGroupName -DataFactoryName $Name -Name $datasetName -File $filePath -Force -ErrorAction Stop"
			$result = New-AzureRmDataFactoryDataset -ResourceGroupName $ResourceGroupName -DataFactoryName $Name -File $filePath -Force -ErrorAction Stop
			Write-LogInformation "Deploy-Dataset: correctly deployed the dataset $datasetName from: $filePath"
			return $true
		}
		catch
		{
			Write-LogError "Deploy-Dataset: Could not deploy the dataset from: $filePath ! Error: `n$_"
			return $false
		}
}

# Deploys an individual pipeline for the ADF
function Deploy-Pipeline
{
		param(
		[Parameter(Mandatory=$true)]
		[ValidateScript({Test-Path $_ -PathType 'Leaf'})]
		[string]$filePath)

		$pipelineName = [io.path]::GetFileNameWithoutExtension($filePath)
		try
		{
			Write-LogVerbose "Deploy-Pipeline: Running the following command: `nNew-AzureRmDataFactoryDataset -ResourceGroupName $ResourceGroupName -DataFactoryName $Name -Name $pipelineName -File $filePath -Force -ErrorAction Stop"
			$result = New-AzureRmDataFactoryPipeline -ResourceGroupName $ResourceGroupName -DataFactoryName $Name -File $filePath -Force -ErrorAction Stop
			Write-LogInformation "Deploy-Pipeline: correctly deployed the pipeline $pipelineName from: $filePath"
			return $true
		}
		catch
		{
			Write-LogError "Deploy-Pipeline: Could not deploy the pipeline from: $filePath ! Error: `n$_"
			return $false
		}
}

# Deleted the ADF, returns $true on success, $false otherwise
function Delete-AzureDataFactory
{
	Write-LogInformation "Started function: $($MyInvocation.MyCommand)"

	if( -not (Get-AzureDataFactory -HideErrors) )
	{
		Write-LogError "Delete-AzureDataFactory: The ADF: $Name does not exist and thus can not be removed!"
		return $false
	}

	Write-LogVerbose "Attempting to remove the ADF: $Name"

	try
	{
		Write-LogVerbose "Delete-AzureDataFactory: Running the following command: `nRemove-AzureRmDataFactory -ResourceGroupName $ResourceGroupName -Name $Name -Force"
		Remove-AzureRmDataFactory -ResourceGroupName $ResourceGroupName -Name $Name -Force
	}
	catch
	{
		Write-LogError "Delete-AzureDataFactory: Could not remove the ADF: $Name !"
		return $false
	}

	Write-LogInformation "Finished function: $($MyInvocation.MyCommand)"
	return $true
}

# Checks running environment to see if progress: info/verbose messages will be displayed
# ErrorActionPreference and WarningPreference are by default set to "Continue" in PS environments, not checking these
function Check-Environment
{
	Write-LogInformation "Started function: $($MyInvocation.MyCommand)"

	if($InformationPreference -ne "Continue")
	{
		Write-LogWarning "
`$InformationPreference is set to '$InformationPreference'.
You will not see any progress information in this script unless you
add -InformationAction `"Continue`" to your script call or set
`$InformationPreference = `"Continue`" in your PS environment to see Information messages`n`n"
	}
	else
	{
		Write-LogVerbose "`$InformationPreference => $InformationPreference"
	}

	if($VerbosePreference -ne "Continue")
	{
		Write-LogWarning "
`$VerbosePreference is set to '$VerbosePreference'.
You will not see any progress information in this script! 
Please add -VerboseAction `"Continue`" or simply -Verbose to your script call or set
`$VerbosePreference = `"Continue`" in your PS environment to see Verbose messages`n`n"
	}
	else
	{
		Write-LogVerbose "`$VerbosePreference => $VerbosePreference"
	}

	Write-LogInformation "Finished function: $($MyInvocation.MyCommand)"
}

# Gets the date used in logging functions in a short format
function Get-LogDate
{
	return Get-Date -Format s
}

# Writes the error prefixed with a timestamp
function Write-LogError
{
	param(
	[Parameter(Mandatory=$true)]
	[ValidateScript({ -not ([string]::IsNullOrWhiteSpace($_)) })]
	[string]$message)
	Write-Error "$(Get-LogDate) `t $message"
}

# Writes the warning prefixed with a timestamp
function Write-LogWarning
{
	param(
	[Parameter(Mandatory=$true)]
	[ValidateScript({ -not ([string]::IsNullOrWhiteSpace($_)) })]
	[string]$message)
	Write-Warning "$(Get-LogDate) `t $message"
}

# Writes the information prefixed with a timestamp
function Write-LogInformation
{
	param(
	[Parameter(Mandatory=$true)]
	[ValidateScript({ -not ([string]::IsNullOrWhiteSpace($_)) })]
	[string]$message)
    Write-Information "$(Get-LogDate) `t $message"
}

# Writes the verbose message prefixed with a timestamp
function Write-LogVerbose
{
	param(
	[Parameter(Mandatory=$true)]
	[ValidateScript({ -not ([string]::IsNullOrWhiteSpace($_)) })]
	[string]$message)
    Write-Verbose "$(Get-LogDate) `t $message"
}


# Main - entry point to the actual script. Used Main pattern to be able to declare functions after their usage, for readability
Write-LogInformation "Started script"
Main
Write-LogInformation "Finished script"