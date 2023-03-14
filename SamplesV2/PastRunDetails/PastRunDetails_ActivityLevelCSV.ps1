$dataFactoryName = ""
$resourceGroupName = ""
$startTime = "1/12/2023 00:00:00"
$endTime = "1/13/2023 00:00:00"
$filePath = "C:\temp\adfouput.csv"

$pipelineRuns = Get-AzDataFactoryV2PipelineRun -ResourceGroupName $resourceGroupName -DataFactoryName $dataFactoryName -LastUpdatedAfter $startTime -LastUpdatedBefore $endTime

$activityDetails = @()

foreach($pipelineRun in $pipelineRuns) {
    
    $activtiyRuns = Get-AzDataFactoryV2ActivityRun -ResourceGroupName $resourceGroupName -DataFactoryName $dataFactoryName -pipelineRunId $pipelineRun.RunId -RunStartedAfter $startTime -RunStartedBefore $endTime
    
    foreach($activtiyRun in $activtiyRuns) {
        if ($null -ne $activtiyRun.Output -and
                $null -ne $activtiyRun.Output.SelectToken("billingReference.billableDuration")) {
            
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
        }
    }
}

$activityDetails | Export-Csv -Path $filePath