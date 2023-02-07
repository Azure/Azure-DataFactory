<#
.SYNOPSIS
    Stop/ start triggers during release process (CICD)
.DESCRIPTION
    The script can be used to stop triggers before deployment and restrat them afterward. It stops the trigger only if the trigger is in started state and there is a change in trigger
.PARAMETER ArmTemplate
    Arm template file path
        example: C:\Adf\ArmTemlateOutput\ARMTemplateForFactory.json
.PARAMETER ResourceGroupName
    Name of the resource group where the factory resource is in
.PARAMETER DataFactoryName
    Name of the data factory being deployed
.PARAMETER PreDeployment
    Default: $true
    True: Runs script as pre-deployment so it will stop triggers prior to deployment
    False: Runs script as post-deployment so it will delete the removed resources and start the triggers
.PARAMETER DeleteDeployment
    Default: $false
    Clean-up the deployment labels on the resource group
.PARAMETER ArmTemplateParameters
    Default: $null
    Arm template parameters file path
        example: C:\Adf\ArmTemlateOutput\ARMTemplateParametersForFactory.json
    If not provided, the script will try to find the file with name ARMTemplateParametersForFactory.json in ArmTemplate folder path
.PARAMETER ExplicitStopTriggerList
    Default: @() (An empty array)
    Expliticly stops the triggers form this list if the trigger is in started state even if the trigger payload not changed
        example: "testTrigger", "storageEventsTrigger"
    The script is not very comprehensive in detecting the trigger changes, so this parameter can be used to stop triggers explicitly if required
.PARAMETER MaxJsonDepth
    Default: 100
    The maximum depth the JSON input is allowed to have
#>
param
(
    [parameter(Mandatory = $true)] [String] $ArmTemplate,
    [parameter(Mandatory = $true)] [String] $ResourceGroupName,
    [parameter(Mandatory = $true)] [String] $DataFactoryName,
    [parameter(Mandatory = $false)] [Bool] $PreDeployment = $true,
    [parameter(Mandatory = $false)] [Bool] $DeleteDeployment = $false,
    [parameter(Mandatory = $false)] [String] $ArmTemplateParameters = $null,
    [parameter(Mandatory = $false)] [String[]] $ExplicitStopTriggerList = @(),
    [parameter(Mandatory = $false)] [int] $MaxJsonDepth = 100
)

function Get-PipelineDependency {
    param(
        [System.Object] $activity
    )
    $result = @()
    if ($activity.Pipeline) {
        $result += $activity.Pipeline.ReferenceName
    }
    elseif ($activity.Activities) {
        $activity.Activities | ForEach-Object { $result += Get-PipelineDependency -activity $_ }
    }
    elseif ($activity.ifFalseActivities -or $activity.ifTrueActivities) {
        $activity.ifFalseActivities | Where-Object { $_ -ne $null } | ForEach-Object { $result += Get-PipelineDependency -activity $_ }
        $activity.ifTrueActivities | Where-Object { $_ -ne $null } | ForEach-Object { $result += Get-PipelineDependency -activity $_ }
    }
    elseif ($activity.defaultActivities) {
        $activity.defaultActivities | ForEach-Object { $result += Get-PipelineDependency -activity $_ }
        if ($activity.cases) {
            $activity.cases | ForEach-Object { $_.activities } | ForEach-Object { $result += Get-PipelineDependency -activity $_ }
        }
    }

    return $result
}

function Push-PipelinesToList {
    param(
        [Microsoft.Azure.Commands.DataFactoryV2.Models.PSPipeline]$pipeline,
        [Hashtable] $pipelineNameResourceDict,
        [Hashtable] $visited,
        [System.Collections.Stack] $sortedList
    )
    if ($visited[$pipeline.Name] -eq $true) {
        return;
    }
    $visited[$pipeline.Name] = $true;
    $pipeline.Activities | ForEach-Object { Get-PipelineDependency -activity $_ -pipelineNameResourceDict $pipelineNameResourceDict }  | ForEach-Object {
        Push-PipelinesToList -pipeline $pipelineNameResourceDict[$_] -pipelineNameResourceDict $pipelineNameResourceDict -visited $visited -sortedList $sortedList
    }
    $sortedList.Push($pipeline)
}

function Get-SortedPipeline {
    param(
        [string] $DataFactoryName,
        [string] $ResourceGroupName
    )
    $pipelines = Get-AzDataFactoryV2Pipeline -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroupName
    $ppDict = @{}
    $visited = @{}
    $stack = new-object System.Collections.Stack
    $pipelines | ForEach-Object { $ppDict[$_.Name] = $_ }
    $pipelines | ForEach-Object { Push-PipelinesToList -pipeline $_ -pipelineNameResourceDict $ppDict -visited $visited -sortedList $stack }
    $sortedList = new-object Collections.Generic.List[Microsoft.Azure.Commands.DataFactoryV2.Models.PSPipeline]

    while ($stack.Count -gt 0) {
        $sortedList.Add($stack.Pop())
    }
    $sortedList
}

function Push-TriggersToList {
    param(
        [Microsoft.Azure.Commands.DataFactoryV2.Models.PSTrigger]$trigger,
        [Hashtable] $triggerNameResourceDict,
        [Hashtable] $visited,
        [System.Collections.Stack] $sortedList
    )
    if ($visited[$trigger.Name] -eq $true) {
        return;
    }
    $visited[$trigger.Name] = $true;
    if ($trigger.Properties.DependsOn) {
        $trigger.Properties.DependsOn | Where-Object { $_ -and $_.ReferenceTrigger } | ForEach-Object {
            Push-TriggersToList -trigger $triggerNameResourceDict[$_.ReferenceTrigger.ReferenceName] -triggerNameResourceDict $triggerNameResourceDict -visited $visited -sortedList $sortedList
        }
    }
    $sortedList.Push($trigger)
}

function Get-SortedTrigger {
    param(
        [string] $DataFactoryName,
        [string] $ResourceGroupName
    )
    $triggers = Get-AzDataFactoryV2Trigger -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName
    $triggerDict = @{}
    $visited = @{}
    $stack = new-object System.Collections.Stack
    $triggers | ForEach-Object { $triggerDict[$_.Name] = $_ }
    $triggers | ForEach-Object { Push-TriggersToList -trigger $_ -triggerNameResourceDict $triggerDict -visited $visited -sortedList $stack }
    $sortedList = new-object Collections.Generic.List[Microsoft.Azure.Commands.DataFactoryV2.Models.PSTrigger]
    while ($stack.Count -gt 0) {
        $sortedList.Add($stack.Pop())
    }
    $sortedList
}

function Get-SortedLinkedService {
    param(
        [string] $DataFactoryName,
        [string] $ResourceGroupName
    )
    $linkedServices = Get-AzDataFactoryV2LinkedService -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName
    $LinkedServiceHasDependencies = @('HDInsightLinkedService', 'HDInsightOnDemandLinkedService', 'AzureBatchLinkedService')
    $Akv = 'AzureKeyVaultLinkedService'
    $HighOrderList = New-Object Collections.Generic.List[Microsoft.Azure.Commands.DataFactoryV2.Models.PSLinkedService]
    $RegularList = New-Object Collections.Generic.List[Microsoft.Azure.Commands.DataFactoryV2.Models.PSLinkedService]
    $AkvList = New-Object Collections.Generic.List[Microsoft.Azure.Commands.DataFactoryV2.Models.PSLinkedService]

    $linkedServices | ForEach-Object {
        if ($_.Properties.GetType().Name -in $LinkedServiceHasDependencies) {
            $HighOrderList.Add($_)
        }
        elseif ($_.Properties.GetType().Name -eq $Akv) {
            $AkvList.Add($_)
        }
        else {
            $RegularList.Add($_)
        }
    }

    $SortedList = New-Object Collections.Generic.List[Microsoft.Azure.Commands.DataFactoryV2.Models.PSLinkedService]($HighOrderList.Count + $RegularList.Count + $AkvList.Count)
    $SortedList.AddRange($HighOrderList)
    $SortedList.AddRange($RegularList)
    $SortedList.AddRange($AkvList)
    $SortedList
}

function Compare-TriggerPayload {
    param(
        [Microsoft.Azure.Commands.DataFactoryV2.Models.PSTrigger]$triggerDeployed,
        [PSCustomObject]$triggerInTemplate,
        [PSCustomObject]$templateParameters
    )
    try {
        Write-Host "Comparing '$($triggerDeployed.Name)' trigger payload"
        # Parse the trigger json from template to deserialize to trigger object
        $triggerInTemplate.properties.typeProperties | Get-Member -MemberType NoteProperty | ForEach-Object {
            $triggerInTemplate.properties | Add-Member -NotePropertyName $_.Name -NotePropertyValue $triggerInTemplate.properties.typeProperties.$($_.Name) -Force
        }
        $addPropDictionary = New-Object "System.Collections.Generic.Dictionary[System.String, System.Object]"
        $addPropDictionary.Add('typeProperties', $triggerInTemplate.properties.typeProperties)
        $triggerInTemplate.properties | Add-Member -NotePropertyName 'additionalProperties' -NotePropertyValue $addPropDictionary
        $triggerInTemplate.properties.PSObject.Properties.Remove('typeProperties')
        $triggerTemplateJson = ConvertTo-Json -InputObject $triggerInTemplate.properties -Depth $MaxJsonDepth -EscapeHandling Default
        $updatedTemplateJson = Update-TriggerTemplate -templateJson $triggerTemplateJson -templateParameters $templateParameters
        $serializerOptions = New-Object System.Text.Json.JsonSerializerOptions -Property @{ PropertyNameCaseInsensitive = $True }
        $payloadPSObject = $updatedTemplateJson | ConvertFrom-Json -Depth $MaxJsonDepth
        if ($triggerDeployed.Properties.RuntimeState -ne $payloadPSObject.runtimeState) {
            Write-Host "Change detected in '$($triggerDeployed.Name)' trigger payload - runtimeState changed"
            return $True;
        }

        if ($triggerDeployed.Properties.GetType().Name -eq [Microsoft.Azure.Management.DataFactory.Models.ScheduleTrigger].Name) {
            # DayOfWeek needs to have enum value instead of enum strings
            if ($payloadPSObject.recurrence.schedule.weekDays) {
                [System.Array]$payloadPSObject.recurrence.schedule.weekDays = $payloadPSObject.recurrence.schedule.weekDays | ForEach-Object { ([System.DayOfWeek]::$_).value__ }
            }
            if ($payloadPSObject.recurrence.schedule.monthlyOccurrences) {
                $payloadPSObject.recurrence.schedule.monthlyOccurrences | ForEach-Object { $_.day = ([System.DayOfWeek]::$($_.day)).value__ }
            }
            $updatedTemplateJson = ConvertTo-Json -InputObject $payloadPSObject -Depth $MaxJsonDepth

            $triggerPayload = [System.Text.Json.JsonSerializer]::Deserialize($updatedTemplateJson,
                [Microsoft.Azure.Management.DataFactory.Models.ScheduleTrigger],
                $serializerOptions)
            return Compare-ScheduleTrigger -triggerDeployed $triggerDeployed -triggerPayload $triggerPayload
        }
        elseif ($triggerDeployed.Properties.GetType().Name -eq [Microsoft.Azure.Management.DataFactory.Models.TumblingWindowTrigger].Name) {
            $triggerPayload = [System.Text.Json.JsonSerializer]::Deserialize($updatedTemplateJson,
                [Microsoft.Azure.Management.DataFactory.Models.TumblingWindowTrigger],
                $serializerOptions)
            return Compare-TumblingWindowTrigger -triggerDeployed $triggerDeployed -triggerPayload $triggerPayload
        }
        elseif ($triggerDeployed.Properties.GetType().Name -eq [Microsoft.Azure.Management.DataFactory.Models.BlobEventsTrigger].Name) {
            $triggerPayload = [System.Text.Json.JsonSerializer]::Deserialize($updatedTemplateJson,
                [Microsoft.Azure.Management.DataFactory.Models.BlobEventsTrigger],
                $serializerOptions)
            return Compare-BlobEventsTrigger -triggerDeployed $triggerDeployed -triggerPayload $triggerPayload
        }
        elseif ($triggerDeployed.Properties.GetType().Name -eq [Microsoft.Azure.Management.DataFactory.Models.CustomEventsTrigger].Name) {
            $triggerPayload = [System.Text.Json.JsonSerializer]::Deserialize($updatedTemplateJson,
                [Microsoft.Azure.Management.DataFactory.Models.CustomEventsTrigger],
                $serializerOptions)
            return Compare-CustomEventsTrigger -triggerDeployed $triggerDeployed -triggerPayload $triggerPayload
        }

        Write-Host "##[warning] Comparison terminated as trigger type '$($triggerDeployed.Properties.GetType().Name)' not match"
        return $True
    }
    catch {
        Write-Host "##[warning] Unable to compare '$($triggerDeployed.Name)' trigger payload, this is not a failure. You can post the issue to https://github.com/Azure/Azure-DataFactory/issues to check if this is user error or limitation."
        Write-Host "##[warning] $_ from Line: $($_.InvocationInfo.ScriptLineNumber)"
        return $True;
    }
}

function Compare-ScheduleTrigger {
    param(
        [Microsoft.Azure.Commands.DataFactoryV2.Models.PSTrigger]$triggerDeployed,
        [Microsoft.Azure.Management.DataFactory.Models.ScheduleTrigger]$triggerPayload
    )
    # Compare if any common trigger properties changed
    $deployedTriggerProps = $triggerDeployed.Properties
    $descriptionChanges = Compare-Object -ReferenceObject $deployedTriggerProps  -DifferenceObject $triggerPayload -Property Description
    $annotationChanges = Compare-Object -ReferenceObject $deployedtriggerProps.Annotations -DifferenceObject $triggerPayload.Annotations

    # Compare if the recurrence changed
    $recurrencechanges = Compare-Object -ReferenceObject $deployedTriggerProps.Recurrence -DifferenceObject $triggerPayload.Recurrence `
        -Property Frequency, Interval, StartTime, EndTime, TimeZone

    # Compare if the schedule changed
    $scheduleChanged = $True;
    if ($null -ne $deployedTriggerProps.Recurrence.Schedule -and $null -ne $triggerPayload.Recurrence.Schedule) {
        $changes = Compare-Object -ReferenceObject $deployedTriggerProps.Recurrence.Schedule -DifferenceObject $triggerPayload.Recurrence.Schedule `
            -Property Minutes, Hours, WeekDays, MonthDays

        # Compare monthly occurrences
        if ($null -eq $changes) {
            $scheduleChanged = $True;
            if ($null -ne $deployedTriggerProps.Recurrence.Schedule.MonthlyOccurrences -and $null -ne $triggerPayload.Recurrence.Schedule.MonthlyOccurrences) {
                $changes =Compare-Object -ReferenceObject $deployedTriggerProps.Recurrence.Schedule.MonthlyOccurrences -DifferenceObject $triggerPayload.Recurrence.Schedule.MonthlyOccurrences `
                    -Property Day, Occurrence
            } elseif ($null -eq $deployedTriggerProps.Recurrence.Schedule.MonthlyOccurrences -and $null -eq $triggerPayload.Recurrence.Schedule.MonthlyOccurrences) {
                $scheduleChanged = $False;
            }
        }

        $scheduleChanged = $null -ne $changes
    } elseif ($null -eq $deployedTriggerProps.Recurrence.Schedule -and $null -eq $triggerPayload.Recurrence.Schedule) {
        $scheduleChanged = $False;
    }

    # Compare to check if there is any change in referenced pipeline
    $pipelineRefChanged = Compare-TriggerPipelineReference -tprDeployed $deployedTriggerProps.Pipelines -tprPayload $triggerPayload.Pipelines

    # Temporary workaround for Get-AzDataFactoryV2Trigger returning incorrect datetime notation
    # https://github.com/Azure/azure-powershell/issues/20474 issue, thanks to gvdmaaden for this workaround
    Update-TriggerTimeFormat -deployedAdditionalProps $deployedTriggerProps.AdditionalProperties

    # Compare additional properties (unmatched properties stay here)
    $additionalPropsChanged = Compare-TriggerAdditionalProperty -deployedAdditionalProps $deployedTriggerProps.AdditionalProperties `
        -payloadAdditionalProps $triggerPayload.AdditionalProperties

    if (($null -ne $descriptionChanges) -or ($null -ne $annotationChanges) -or ($null -ne $recurrencechanges) -or `
            $scheduleChanged -or $pipelineRefChanged -or $additionalPropsChanged) {
        Write-Host "Change detected in '$($triggerDeployed.Name)' trigger payload - descriptionChanges=$($descriptionChanges.Length), annotationChanges=$($annotationChanges.Length), recurrencechanges=$($recurrencechanges.Length), scheduleChanged=$scheduleChanged, pipelineRefChanged=$pipelineRefChanged, additionalPropsChanged=$additionalPropsChanged"
        return $True
    }

    Write-Host "No change detected in '$($triggerDeployed.Name)' trigger payload"
    return $False;
}

function Compare-TumblingWindowTrigger {
    param(
        [Microsoft.Azure.Commands.DataFactoryV2.Models.PSTrigger]$triggerDeployed,
        [Microsoft.Azure.Management.DataFactory.Models.TumblingWindowTrigger]$triggerPayload
    )
    # Compare if any of common tumbling window trigger properties changed
    $propertyChanges = Compare-Object -ReferenceObject $triggerDeployed.Properties -DifferenceObject $triggerPayload `
        -Property Frequency, Interval, StartTime, EndTime, Delay, MaxConcurrency, Description
    $annotationChanges = Compare-Object -ReferenceObject $triggerDeployed.Properties.Annotations -DifferenceObject $triggerPayload.Annotations
    $retryPolicyChanges = Compare-Object -ReferenceObject $triggerDeployed.Properties.RetryPolicy -DifferenceObject $triggerPayload.RetryPolicy `
        -Property Count, IntervalInSeconds

    # Compare to check if there is any change in referenced pipeline
    $tprDeployed = New-Object System.Collections.Generic.List[Microsoft.Azure.Management.DataFactory.Models.TriggerPipelineReference]
    $tprDeployed.Add($triggerDeployed.Properties.Pipeline)
    $tprPayload = New-Object System.Collections.Generic.List[Microsoft.Azure.Management.DataFactory.Models.TriggerPipelineReference]
    $tprPayload.Add($triggerPayload.Pipeline)
    $pipelineRefChanged = Compare-TriggerPipelineReference -tprDeployed $tprDeployed -tprPayload $tprPayload

    # Compare additional properties (unmatched properties stay here ex: DependsOn)
    $additionalPropsChanged = Compare-TriggerAdditionalProperty -deployedAdditionalProps $triggerDeployed.Properties.AdditionalProperties `
        -payloadAdditionalProps $triggerPayload.AdditionalProperties

    if (($null -ne $propertyChanges) -or ($null -ne $annotationChanges) -or ($null -ne $retryPolicyChanges) -or `
        $pipelineRefChanged -or $additionalPropsChanged) {
        Write-Host "Change detected in '$($triggerDeployed.Name)' trigger payload - propertyChanges=$($propertyChanges.Length), annotationChanges=$($annotationChanges.Length), retryPolicyChanges=$($retryPolicyChanges.Length), pipelineRefChanged=$pipelineRefChanged, additionalPropsChanged=$additionalPropsChanged"
        return $True
    }

    Write-Host "No change detected in '$($triggerDeployed.Name)' trigger payload"
    return $False;
}

function Compare-BlobEventsTrigger {
    param(
        [Microsoft.Azure.Commands.DataFactoryV2.Models.PSTrigger]$triggerDeployed,
        [Microsoft.Azure.Management.DataFactory.Models.BlobEventsTrigger]$triggerPayload
    )
    $propertyChanges = Compare-Object -ReferenceObject $triggerDeployed.Properties -DifferenceObject $triggerPayload `
        -Property BlobPathBeginsWith, BlobPathEndsWith, IgnoreEmptyBlobs, Events, Scope, Description
    $annotationChanges = Compare-Object -ReferenceObject $triggerDeployed.Properties.Annotations -DifferenceObject $triggerPayload.Annotations

    # Compare to check if there is any change in referenced pipeline
    $pipelineRefChanged = Compare-TriggerPipelineReference -tprDeployed $triggerDeployed.Properties.Pipelines -tprPayload $triggerPayload.Pipelines

    # Compare additional properties (unmatched properties stay here - ex: advancedFilters)
    $additionalPropsChanged = Compare-TriggerAdditionalProperty -deployedAdditionalProps $triggerDeployed.Properties.AdditionalProperties `
        -payloadAdditionalProps $triggerPayload.AdditionalProperties

    if (($null -ne $propertyChanges) -or ($null -ne $annotationChanges) -or $pipelineRefChanged -or $additionalPropsChanged) {
        Write-Host "Change detected in '$($triggerDeployed.Name)' trigger payload - propertyChanges=($propertyChanges.Length), annotationChanges=$($annotationChanges.Length), pipelineRefChanged=$pipelineRefChanged, additionalPropsChanged=$additionalPropsChanged"
        return $True
    }

    Write-Host "No change detected in '$($triggerDeployed.Name)' trigger payload"
    return $False;
}

function Compare-CustomEventsTrigger {
    param(
        [Microsoft.Azure.Commands.DataFactoryV2.Models.PSTrigger]$triggerDeployed,
        [Microsoft.Azure.Management.DataFactory.Models.CustomEventsTrigger]$triggerPayload
    )
    # Compare common and event properties
    $propertyChanges = Compare-Object -ReferenceObject $triggerDeployed.Properties -DifferenceObject $triggerPayload `
        -Property SubjectBeginsWith, SubjectEndsWith, Scope, Description
    $eventChanges = Compare-Object -ReferenceObject $triggerDeployed.Properties.Events -DifferenceObject $triggerPayload.Events
    $annotationChanges = Compare-Object -ReferenceObject $triggerDeployed.Properties.Annotations -DifferenceObject $triggerPayload.Annotations


    # Compare to check if there is any change in referenced pipeline
    $pipelineRefChanged = Compare-TriggerPipelineReference -tprDeployed $triggerDeployed.Properties.Pipelines -tprPayload $triggerPayload.Pipelines

    # Compare additional properties (unmatched properties stay here - ex: advancedFilters)
    $additionalPropsChanged = Compare-TriggerAdditionalProperty -deployedAdditionalProps $triggerDeployed.Properties.AdditionalProperties `
        -payloadAdditionalProps $triggerPayload.AdditionalProperties

    if (($null -ne $propertyChanges) -or ($null -ne $eventChanges) -or ($null -ne $annotationChanges) -or `
        $pipelineRefChanged -or $additionalPropsChanged) {
        Write-Host "Change detected in '$($triggerDeployed.Name)' trigger payload - propertyChanges=$($propertyChanges.Length), eventChanges=$($eventChanges.Length), annotationChanges=$($annotationChanges.Length), pipelineRefChanged=$pipelineRefChanged, additionalPropsChanged=$additionalPropsChanged"
        return $True
    }

    Write-Host "No change detected in '$($triggerDeployed.Name)' trigger payload"
    return $False;
}

function Compare-TriggerPipelineReference {
    param(
        [System.Collections.Generic.IList[Microsoft.Azure.Management.DataFactory.Models.TriggerPipelineReference]]$tprDeployed,
        [System.Collections.Generic.IList[Microsoft.Azure.Management.DataFactory.Models.TriggerPipelineReference]]$tprPayload
    )
    # Compare to check if there is any change in referenced pipeline
    $pipelineRefchanged = $True;
    if ($null -ne $tprDeployed.PipelineReference -and $null -ne $tprPayload.PipelineReference) {
        $changes = Compare-Object -ReferenceObject $tprDeployed.PipelineReference -DifferenceObject $tprPayload.PipelineReference `
            -Property Name, ReferenceName
        $pipelineRefchanged = $changes.Length -gt 0
    }
    elseif ($null -eq $tprDeployed.PipelineReference -and $null -eq $tprPayload.PipelineReference) {
        $pipelineRefchanged = $False;
    }

    # If no change in pipeline reference, compare to check if there is any change in pipeline parameters
    $paramsChanged = $True
    if (!$pipelineRefchanged -and $tprPayload.Count -gt 0) {
        $paramsChanged = $False
        for ($counter = 0; $counter -lt $tprPayload.count; $counter++) {
            $pipelineReferenceName = $tprPayload[$counter].PipelineReference.ReferenceName
            $payloadPipelineRef = $tprPayload | Where-Object { $_.PipelineReference.ReferenceName -eq $pipelineReferenceName }
            $deployedPipelineRef = $tprDeployed | Where-Object { $_.PipelineReference.ReferenceName -eq $pipelineReferenceName }
            if ($deployedPipelineRef.Parameters.Keys.Count -eq $payloadPipelineRef.Parameters.Keys.Count) {
                foreach ($key in $deployedPipelineRef.Parameters.Keys) {
                    $deployedValue = $null
                    $payloadValue = $null
                    if (!$payloadPipelineRef.Parameters.TryGetValue($key, [ref]$payloadValue) -or
                        !$deployedPipelineRef.Parameters.TryGetValue($key, [ref]$deployedValue) ) {
                        $paramsChanged = $True
                        break
                    }
                    else {
                        if ($deployedValue.GetType().Name -in @("JObject", "JArray")) {
                            if (($deployedValue.ToString() | ConvertFrom-Json).type -eq "SecureString") {
                                $paramsChanged = $True
                                Write-Host "##[warning] SecureString parameter is always treated as a change"
                                break
                            } else {
                                $paramValueChanges = Compare-Object -ReferenceObject ($deployedValue.ToString() | ConvertFrom-Json) -DifferenceObject ($payloadValue.ToString() | ConvertFrom-Json)
                            }
                        } elseif ($deployedValue.GetType().Name -eq "Boolean") {
                            $paramValueChanges = Compare-Object -ReferenceObject ($deployedValue.ToString().ToLower() | ConvertFrom-Json) -DifferenceObject ($payloadValue.ToString().ToLower() | ConvertFrom-Json)
                        } else {
                            $paramValueChanges = Compare-Object -ReferenceObject $deployedValue -DifferenceObject $payloadValue
                        }
                        if ($paramValueChanges.Length -gt 0) {
                            $paramsChanged = $True
                            break
                        }
                    }
                }
            }
            else {
                $paramsChanged = $True
                break;
            }
        }
    }

    return $pipelineRefchanged -or $paramsChanged
}

function Compare-TriggerAdditionalProperty {
    param(
        [System.Collections.Generic.Dictionary[String, System.Object]]$deployedAdditionalProps,
        [System.Collections.Generic.Dictionary[String, System.Object]]$payloadAdditionalProps
    )
    $additionalPropchanged = $True;
    if ($null -ne $deployedAdditionalProps -and $null -ne $payloadAdditionalProps) {
        $changes = Compare-Object -ReferenceObject $deployedAdditionalProps -DifferenceObject $payloadAdditionalProps `
            -Property Keys
        $additionalPropchanged = $null -ne $changes
        if (-not $additionalPropchanged) {
            foreach ($key in $deployedAdditionalProps.Keys) {
                $deployedValue = $null; $payloadValue = $null;
                if (!$payloadAdditionalProps.TryGetValue($key, [ref]$payloadValue) -or
                    !$deployedAdditionalProps.TryGetValue($key, [ref]$deployedValue)) {
                    $additionalPropchanged = $True
                    break
                }
                else {
                    $payloadJObect = [Newtonsoft.Json.Linq.JObject]::Parse($payloadValue)
                    $additionalPropValueChanges = Compare-Object -ReferenceObject $deployedValue -DifferenceObject $payloadJObect
                    if ($null -ne $additionalPropValueChanges) {
                        $additionalPropchanged = $True
                        break
                    }
                }
            }
        }
    }
    elseif ($null -eq $deployedAdditionalProps -and $null -eq $payloadAdditionalProps) {
        $additionalPropchanged = $False;
    }

    return $additionalPropchanged
}

function Update-TriggerTimeFormat {
    param(
        [System.Collections.Generic.Dictionary[String, System.Object]]$deployedAdditionalProps
    )
    foreach ($key in $deployedAdditionalProps.Keys) {
        $deployedValue = $null;
        if ($deployedAdditionalProps.TryGetValue($key, [ref]$deployedValue) -and $deployedValue["recurrence"]["timeZone"] -ne "UTC")
        {
            if ($null -ne $deployedValue["recurrence"]["startTime"]) {
                $startTimeString = $deployedValue["recurrence"]["startTime"].ToString("yyyy-MM-ddTHH:mm:ss");
                $startDateTime = Get-Date -Date $startTimeString;
                $deployedValue["recurrence"]["startTime"] = New-Object DateTime 2000, 1, 1, 1, 0, 0, ([DateTimeKind]::Unspecified)
                $deployedValue["recurrence"]["startTime"] = New-Object DateTime $startDateTime.Year, $startDateTime.Month, $startDateTime.Day, $startDateTime.Hour, $startDateTime.Minute, $startDateTime.Second, ([DateTimeKind]::Unspecified)
            }

            if ($null -ne $deployedValue["recurrence"]["endTime"]) {
                $endTimeString = $deployedValue["recurrence"]["endTime"].ToString("yyyy-MM-ddTHH:mm:ss");
                $endDateTime = Get-Date -Date $endTimeString;
                $deployedValue["recurrence"]["endTime"] = New-Object DateTime 2000, 1, 1, 1, 0, 0, ([DateTimeKind]::Unspecified)
                $deployedValue["recurrence"]["endTime"] = New-Object DateTime $endDateTime.Year, $endDateTime.Month, $endDateTime.Day, $endDateTime.Hour, $endDateTime.Minute, $endDateTime.Second, ([DateTimeKind]::Unspecified)
            }
        }
    }
}

function Update-TriggerTemplate {
    param(
        [string]$templateJson,
        [PSCustomObject]$templateParameters
    )
    $parameterMatches = [System.Text.RegularExpressions.Regex]::Matches($templateJson, '\[parameters\([^)]*\)\]')
    foreach ($parameterMatch in $parameterMatches) {
        [string]$parameterName = $parameterMatch.Value.Substring(13, $parameterMatch.Value.Length - 16)
        if ($null -ne $templateParameters.$($parameterName) && ($PreDeployment || $parameterName.EndsWith('runtimeState'))) {
            $parameterType = $null -ne $templateParameters.$($parameterName).value ? $templateParameters.$($parameterName).value.GetType().Name : $null
            if ($parameterType -eq 'Object[]' -or $parameterType -eq 'PSCustomObject') {
                $parameterValue = ConvertTo-Json $templateParameters.$($parameterName).value -Depth $MaxJsonDepth -EscapeHandling Default
                $templateJson = $templateJson -replace [System.Text.RegularExpressions.Regex]::Escape("`"$($parameterMatch.Value)`""), $parameterValue
            } elseif ($parameterType -eq 'Int64') {
                $templateJson = $templateJson -replace [System.Text.RegularExpressions.Regex]::Escape("`"$($parameterMatch.Value)`""), $templateParameters.$($parameterName).value
            } elseif ($parameterType -eq 'Boolean') {
                $templateJson = $templateJson -replace [System.Text.RegularExpressions.Regex]::Escape("`"$($parameterMatch.Value)`""), $templateParameters.$($parameterName).value.ToString().ToLower()
            } else {
                $templateJson = $templateJson -replace [System.Text.RegularExpressions.Regex]::Escape($parameterMatch.Value), $templateParameters.$($parameterName).value
            }
        }
    }

    return $templateJson
}

try {
    # Show warning message that PowerShell Core or PowerShell version > 7.0 is required
    $PSCompatible = $True
    if ($PSVersionTable.PSEdition -ne 'Core' -and ([System.Version]$PSVersionTable.PSVersion -lt [System.Version]"7.0.0")) {
        $PSCompatible = $False
        Write-Host "##[warning] The script is not compatible with your current PowerShell version $($PSVersionTable.PSVersion). Use either PowerShell Core or at least PS version 7.0, otherwise the script may fail to compare the trigger payload and always stop/start the trigger(s)"
    }

    $templateJson = Get-Content $ArmTemplate | ConvertFrom-Json
    $resources = $templateJson.resources

    if (-not $ArmTemplateParameters) {
        $ArmTemplateParameters = Join-Path -Path (Split-Path $ArmTemplate -Parent) -ChildPath 'ArmTemplateParametersForFactory.json'
        Write-Host "##[warning] Arm-template parameter file path not specified, the script will look for the file in arm-template file path."
    }

    $templateParameters = $null
    if (Test-Path -Path $ArmTemplateParameters) {
        $templateParametersJson = Get-Content $ArmTemplateParameters | ConvertFrom-Json
        $templateParameters = $templateParametersJson.parameters
    } else {
        Write-Host "##[warning] The script couldn't find the arm-tempalte parameter file in the arm-template file path, the trigger comparison won't work for parameterized properties. Please pass the arm-template parameter file path to ArmTemplateParameters script argument."
    }

    #Triggers
    Write-Host "Getting triggers"
    $triggersInTemplate = $resources | Where-Object { $_.type -eq "Microsoft.DataFactory/factories/triggers" }
    $triggerNamesInTemplate = $triggersInTemplate | ForEach-Object { $_.name.Substring(37, $_.name.Length - 40) }

    $triggersDeployed = Get-SortedTrigger -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroupName

    if ($PreDeployment -eq $true) {
        #Stop trigger only if there is change in payload
        $triggersToStop = $triggersDeployed | Where-Object { $_.Name -in $triggerNamesInTemplate -and $_.RuntimeState -ne 'Stopped' } `
        | Where-Object {
            $triggerName = $_.Name;
            $triggerInTemplate = $triggersInTemplate | Where-Object { $_.name.Substring(37, $_.name.Length - 40) -eq $triggerName };
            Compare-TriggerPayload -triggerDeployed $_ -triggerInTemplate $triggerInTemplate -templateParameters $templateParameters
        } `
        | ForEach-Object {
            New-Object PSObject -Property @{
                Name        = $_.Name
                TriggerType = $_.Properties.GetType().Name
            }
        }

        Write-Host "Stopping $($triggersToStop.Count) triggers  `n"
        $triggersToStop | ForEach-Object {
            if ($_.TriggerType -eq 'BlobEventsTrigger') {
                Write-Host "Unsubscribing $($_.Name) from events"
                $status = Remove-AzDataFactoryV2TriggerSubscription -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name
                while ($status.Status -ne 'Disabled') {
                    Start-Sleep -s 15
                    $status = Get-AzDataFactoryV2TriggerSubscriptionStatus -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name
                }
            }
            Write-Host "Stopping trigger $($_.Name)"
            Stop-AzDataFactoryV2Trigger -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name -Force
        }

        $explicitTriggersToStop = $triggersDeployed | Where-Object { $_.Name -in $triggerNamesInTemplate -and $_.RuntimeState -ne 'Stopped' } `
        | Where-Object { $_.Name -in $ExplicitStopTriggerList } `
        | ForEach-Object {
            New-Object PSObject -Property @{
                Name        = $_.Name
                TriggerType = $_.Properties.GetType().Name
            }
        }

        if ($explicitTriggersToStop -and $explicitTriggersToStop.Count -gt 0) {
            Write-Host "Stopping $($explicitTriggersToStop.Count) triggers from explicit stop-trigger list `n"
            $explicitTriggersToStop | ForEach-Object {
                if ($_.TriggerType -eq 'BlobEventsTrigger') {
                    Write-Host "Unsubscribing $($_.Name) from events"
                    $status = Remove-AzDataFactoryV2TriggerSubscription -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name
                    while ($status.Status -ne 'Disabled') {
                        Start-Sleep -s 15
                        $status = Get-AzDataFactoryV2TriggerSubscriptionStatus -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name
                    }
                }
                Write-Host "Stopping trigger $($_.Name)"
                Stop-AzDataFactoryV2Trigger -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name -Force
            }
        } elseif ($ExplicitStopTriggerList -and $ExplicitStopTriggerList.Count -gt 0) {
            Write-Host "No matching trigger (in started state) to stop from explicit stop-trigger list"
        }
    }
    else {
        #Deleted resources
        #pipelines
        Write-Host "Getting pipelines"
        $pipelinesADF = Get-SortedPipeline -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroupName
        $pipelinesTemplate = $resources | Where-Object { $_.type -eq "Microsoft.DataFactory/factories/pipelines" }
        $pipelinesNames = $pipelinesTemplate | ForEach-Object { $_.name.Substring(37, $_.name.Length - 40) }
        $deletedpipelines = $pipelinesADF | Where-Object { $pipelinesNames -notcontains $_.Name }
        #dataflows
        $dataflowsADF = Get-AzDataFactoryV2DataFlow -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroupName
        $dataflowsTemplate = $resources | Where-Object { $_.type -eq "Microsoft.DataFactory/factories/dataflows" }
        $dataflowsNames = $dataflowsTemplate | ForEach-Object { $_.name.Substring(37, $_.name.Length - 40) }
        $deleteddataflow = $dataflowsADF | Where-Object { $dataflowsNames -notcontains $_.Name }
        #datasets
        Write-Host "Getting datasets"
        $datasetsADF = Get-AzDataFactoryV2Dataset -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroupName
        $datasetsTemplate = $resources | Where-Object { $_.type -eq "Microsoft.DataFactory/factories/datasets" }
        $datasetsNames = $datasetsTemplate | ForEach-Object { $_.name.Substring(37, $_.name.Length - 40) }
        $deleteddataset = $datasetsADF | Where-Object { $datasetsNames -notcontains $_.Name }
        #linkedservices
        Write-Host "Getting linked services"
        $linkedservicesADF = Get-SortedLinkedService -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroupName
        $linkedservicesTemplate = $resources | Where-Object { $_.type -eq "Microsoft.DataFactory/factories/linkedservices" }
        $linkedservicesNames = $linkedservicesTemplate | ForEach-Object { $_.name.Substring(37, $_.name.Length - 40) }
        $deletedlinkedservices = $linkedservicesADF | Where-Object { $linkedservicesNames -notcontains $_.Name }
        #Integrationruntimes
        Write-Host "Getting integration runtimes"
        $integrationruntimesADF = Get-AzDataFactoryV2IntegrationRuntime -DataFactoryName $DataFactoryName -ResourceGroupName $ResourceGroupName
        $integrationruntimesTemplate = $resources | Where-Object { $_.type -eq "Microsoft.DataFactory/factories/integrationruntimes" }
        $integrationruntimesNames = $integrationruntimesTemplate | ForEach-Object { $_.name.Substring(37, $_.name.Length - 40) }
        $deletedintegrationruntimes = $integrationruntimesADF | Where-Object { $integrationruntimesNames -notcontains $_.Name }

        #Delete resources
        Write-Host "Deleting triggers"
        $triggersToDelete = $triggersDeployed | Where-Object { $triggerNamesInTemplate -notcontains $_.Name } | ForEach-Object {
            New-Object PSObject -Property @{
                Name        = $_.Name
                TriggerType = $_.Properties.GetType().Name
            }
        }
        $triggersToDelete | ForEach-Object {
            Write-Host "Deleting trigger $($_.Name)"
            $trig = Get-AzDataFactoryV2Trigger -name $_.Name -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName
            if ($trig.RuntimeState -eq 'Started') {
                if ($_.TriggerType -eq 'BlobEventsTrigger') {
                    Write-Host "Unsubscribing trigger $($_.Name) from events"
                    $status = Remove-AzDataFactoryV2TriggerSubscription -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name
                    while ($status.Status -ne 'Disabled') {
                        Start-Sleep -s 15
                        $status = Get-AzDataFactoryV2TriggerSubscriptionStatus -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name
                    }
                }
                Stop-AzDataFactoryV2Trigger -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name -Force
            }
            Remove-AzDataFactoryV2Trigger -Name $_.Name -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Force
        }
        Write-Host "Deleting pipelines"
        $deletedpipelines | ForEach-Object {
            Write-Host "Deleting pipeline $($_.Name)"
            Remove-AzDataFactoryV2Pipeline -Name $_.Name -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Force
        }
        Write-Host "Deleting dataflows"
        $deleteddataflow | ForEach-Object {
            Write-Host "Deleting dataflow $($_.Name)"
            Remove-AzDataFactoryV2DataFlow -Name $_.Name -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Force
        }
        Write-Host "Deleting datasets"
        $deleteddataset | ForEach-Object {
            Write-Host "Deleting dataset $($_.Name)"
            Remove-AzDataFactoryV2Dataset -Name $_.Name -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Force
        }
        Write-Host "Deleting linked services"
        $deletedlinkedservices | ForEach-Object {
            Write-Host "Deleting Linked Service $($_.Name)"
            Remove-AzDataFactoryV2LinkedService -Name $_.Name -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Force
        }
        Write-Host "Deleting integration runtimes"
        $deletedintegrationruntimes | ForEach-Object {
            Write-Host "Deleting integration runtime $($_.Name)"
            Remove-AzDataFactoryV2IntegrationRuntime -Name $_.Name -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Force
        }

        if ($DeleteDeployment -eq $true) {
            Write-Host "Deleting ARM deployment ... under resource group: $ResourceGroupName"
            $deployments = Get-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName
            $deploymentsToConsider = $deployments | Where-Object { $_.DeploymentName -like "ArmTemplate_master*" -or $_.DeploymentName -like "ArmTemplateForFactory*" } | Sort-Object -Property Timestamp -Descending
            $deploymentName = $deploymentsToConsider[0].DeploymentName

            Write-Host "Deployment to be deleted: $deploymentName"
            $deploymentOperations = Get-AzResourceGroupDeploymentOperation -DeploymentName $deploymentName -ResourceGroupName $ResourceGroupName
            $deploymentsToDelete = $deploymentOperations | Where-Object { $_.properties.targetResource.id -like "*Microsoft.Resources/deployments*" }

            $deploymentsToDelete | ForEach-Object {
                Write-Host "Deleting inner deployment: $($_.properties.targetResource.id)"
                Remove-AzResourceGroupDeployment -Id $_.properties.targetResource.id
            }
            Write-Host "Deleting deployment: $deploymentName"
            Remove-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -Name $deploymentName
        }

        #Start active triggers - after cleanup efforts
        $triggersRunning = $triggersDeployed | Where-Object { $_.RuntimeState -eq 'Started' } | ForEach-Object { $_.Name }

        $updatedTriggersInTemplate = $triggersInTemplate
        if ($PSCompatible) {
            try {
                $updatedTriggersInTemplate = $triggersInTemplate | ForEach-Object {
                    $triggerJson = ConvertTo-Json -InputObject $_ -Depth $MaxJsonDepth -EscapeHandling Default
                    Update-TriggerTemplate -templateJson $triggerJson -templateParameters $templateParameters
                } | ConvertFrom-Json -Depth $MaxJsonDepth
            } catch {
                Write-Host "##[warning] Unable to update the parameterized properties in trigger template. The script may fail to start triggers if using parameterized runtimeState."
                $updatedTriggersInTemplate = $triggersInTemplate
            }
        }

        $triggersToStart = $updatedTriggersInTemplate | Where-Object { $_.properties.runtimeState -eq 'Started' -and $_.name.Substring(37, $_.name.Length - 40) -notin $triggersRunning } `
        | Where-Object { $_.properties.pipelines.Count -gt 0 -or $_.properties.pipeline.pipelineReference -ne $null } | ForEach-Object {
            New-Object PSObject -Property @{
                Name        = $_.name.Substring(37, $_.name.Length - 40)
                TriggerType = $_.Properties.type
            }
        }

        if ($triggersToStart.Count -gt 0) {
            Write-Host "Starting $($triggersToStart.Count) triggers"
        }

        $triggersToStart | ForEach-Object {
            if ($_.TriggerType -eq 'BlobEventsTrigger') {
                Write-Host "Subscribing $($_.Name) to events"
                $status = Add-AzDataFactoryV2TriggerSubscription -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name
                while ($status.Status -ne 'Enabled') {
                    Start-Sleep -s 15
                    $status = Get-AzDataFactoryV2TriggerSubscriptionStatus -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name
                }
            }
            Write-Host "Starting trigger $($_.Name)"
            Start-AzDataFactoryV2Trigger -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName -Name $_.Name -Force
        }
    }
} catch {
    Write-Host "##[error] $_ from Line: $($_.InvocationInfo.ScriptLineNumber)"
    throw
}
