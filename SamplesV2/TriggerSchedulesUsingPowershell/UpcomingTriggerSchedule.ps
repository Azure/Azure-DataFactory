# Which Triggers are Scheduled to run, we have to get all pipeline schedules for the trigger, and print them with Pipeline Name, Date, Time

Param(
    [Parameter(Mandatory=$true)] 
    [string] $SubscriptionId, 

    [Parameter(Mandatory=$true)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory=$true)]
    [string] $DataFactoryName,

    [string] $TenantId

)
$baseTriggerUri = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.DataFactory/factories/$DataFactoryName/triggers/"
#Connect-AzAccount -TenantId $TenantId -SubscriptionId $SubscriptionId
$triggerlist = Get-AzDataFactoryV2Trigger -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName

$upcomingRunList = New-Object System.Collections.Generic.List[System.Object]

Write-Host "Number of triggers in ADF: " $triggerlist.length

# For each trigger, find all pipeline schedules and print them 
$triggerlist = Get-AzDataFactoryV2Trigger -ResourceGroupName $ResourceGroupName -DataFactoryName $DataFactoryName

$upcomingRunList = New-Object System.Collections.Generic.List[System.Object]

Write-Host "Number of triggers in ADF: " $triggerlist.length

# For each trigger, find all pipeline schedules and print them 

foreach ( $trigger in $triggerlist ) {  
    $azResourceUri = $baseTriggerUri +"/"+ $trigger.Name
    Write-Host $azResourceUri
}

    $azResource = Get-AzResource -ResourceId $azResourceUri -ExpandProperties

    if ( $azResource.Properties.pipelines.pipelineReference.length -gt 0 ){
        
        #Write-Host "Pipelines"
        #Write-Host "----------"
        #foreach ($pipeline in $azResource.Properties.pipelines.pipelineReference) {
        #    Write-Host  $pipeline.referenceName
        #}
    

        #Write-Host "----------"
        #Write-Host "Recurrence"
        $recurrence =  $azResource.Properties.TypeProperties.recurrence        
        #Write-Host $recurrence
    
        $today = Get-Date
        $upcomingRuns = ""
        $interval = $recurrence.interval
    
        # Determine Future run
        if ( $recurrence.frequency -eq "Minute") {
            $upcomingRuns  = $today.AddMinutes($interval) 
    
        }
        elseif ( $recurrence.frequency -eq "Hour" ) {
            $upcomingRuns = $today.AddHours($interval)
    
        }
        elseif ( $recurrence.frequency -eq "Day" ) {
            $upcomingRuns  = $today.AddDays($interval)
        }
        elseif ($recurrence.frequency -eq "Month") {
            $upcomingRuns = $today.AddMonths($interval)
        }
            
        # Get the Date and Time of the schedule for the trigger of current pipeline
        Write-Host $trigger.Name " is scheduled to run on "   $upcomingRuns.Date " at " $upcomingRuns.TimeOfDay

        # add to the list of upcoming to get all schedules for the pipelines and build new upcoming run list
        $obj = [PSCustomObject]@{
            schedule = $upcomingRuns
            pipelines = $azResource.Properties.pipelines.pipelineReference
        }
        $upcomingRunList.Add($obj )
      
    
    else {
        # no pipelines scheduled for the trigger

    }

}


#$upcomingRunList | Format-Table

Write-Host "Upcoming pipeline runs" -ForegroundColor Green
Write-Host "-----------------------------------------"

$totalUpcomingPipeline = 0
foreach ( $schedules in  $upcomingRunList ) {
    $DateStr = $schedules.schedule.ToString("yyyy-MM-dd")
    Write-Host -ForegroundColor Gray $DateStr  " : Pipelines (" $schedules.pipelines.length ") " $schedules.pipelines.referenceName | Format-List
    $totalUpcomingPipeline += $schedules.pipelines.length
    Write-Host “Pipeline Name is “ $schedules.pipelines.referenceName
    Write-Host “Scheduled Date is “ $schedules.schedule.Date
    Write-Host “Scheduled Time is “ $schedules.schedule.TimeOfDay

}

Write-Host "-----------------------------------------"
Write-Host " Number of upcoming pipeline runs: " $totalUpcomingPipeline 
Write-Host "-----------------------------------------"
