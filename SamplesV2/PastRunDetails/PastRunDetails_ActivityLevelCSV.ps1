$dataFactoryName = ""
$resourceGroupName = ""
$startTime = "1/12/2023 00:00:00"
$endTime = "1/13/2023 12:00:00"
$filePath = "C:\temp\adfouput.csv"

$pipelineRuns = Get-AzDataFactoryV2PipelineRun -ResourceGroupName $resourceGroupName -DataFactoryName $dataFactoryName -LastUpdatedAfter $startTime -LastUpdatedBefore $endTime

$activityDetails = @()

foreach($pipelineRun in $pipelineRuns) {
    
    $activtiyRuns = Get-AzDataFactoryV2ActivityRun -ResourceGroupName $resourceGroupName -DataFactoryName $dataFactoryName -pipelineRunId $pipelineRun.RunId -RunStartedAfter $startTime -RunStartedBefore $endTime
    
    foreach($activtiyRun in $activtiyRuns) {
        if ($activtiyRun.Output -ne $null -and
                $activtiyRun.Output.SelectToken("billingReference.billableDuration") -ne $null) {
            
            $billingReference = $activtiyRun.Output.SelectToken("billingReference.billableDuration").ToString() | ConvertFrom-Json
            $activityBillingType = $activtiyRun.Output.SelectToken("billingReference.activityType").ToString()

            $activityDetails += @{
                meterType = $billingReference.meterType
                duration = $billingReference.duration
                unit = $billingReference.unit
                activityBillingType = $activityBillingType
                activityType = $activtiyRun.ActivityType.ToString()
                activityName = $activtiyRun.ActivityName.ToString()
                activityRunStart = $activtiyRun.ActivityRunStart.ToString()
                pipelineRunId = $pipelineRun.RunId
                pipelineName = $pipelineRun.PipelineName
                dataFactoryName = $pipelineRun.DataFactoryName
            }
            
            # $x = @()
            # $x = $activtiyRun.Output.SelectToken("billingReference.billableDuration").ToString() | ConvertFrom-Json
            # $x | Add-Member -MemberType NoteProperty -Name "activityBillingType" -Value $activtiyRun.Output.SelectToken("billingReference.activityType").ToString()
            # $x | Add-Member -MemberType NoteProperty -Name "activityType" -Value $activtiyRun.ActivityType.ToString()
            # $x | Add-Member -MemberType NoteProperty -Name "activityName" -Value $activtiyRun.ActivityName.ToString()
            # $x | Add-Member -MemberType NoteProperty -Name "activityRunStart" -Value $activtiyRun.ActivityRunStart.ToString()
            # $x | Add-Member -MemberType NoteProperty -Name "pipelineRunId" -Value $pipelineRun.RunId
            # $x | Add-Member -MemberType NoteProperty -Name "pipelineName" -Value $pipelineRun.PipelineName
            # $x | Add-Member -MemberType NoteProperty -Name "dataFactoryName" -Value $pipelineRun.DataFactoryName

            # $activityDetails += $x

        }
    }
}

$activityDetails | Export-Csv -Path $filePath