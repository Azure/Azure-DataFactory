# Interactive sign-in with Azure account credentials
Login-AzureRmAccount

# Prepare resource info
$subId = "..." # Your subscription ID
$resourceGroupName = "..." # Name of the resource group the data factory will be created under
$dataFactoryName = "..." # Name of the Data Factory
$pipelineName = "..." # Name of the pipeline

# Select your subscription 
Select-AzureRmSubscription -SubscriptionId $subId 

# Create the data factory, skip this step or rerun to overwrite if you have existing data factory with same name
Set-AzureRmDataFactoryV2 -ResourceGroupName $resourceGroupName -Name $dataFactoryName -Location "East US"

# Create Linked Services and pipelines
Set-AzureRmDataFactoryV2LinkedService -ResourceGroupName $resourceGroupName -DataFactoryName $dataFactoryName -Name "BatchStorageLinkedService" -File "BatchStorageLinkedService.json"

Set-AzureRmDataFactoryV2LinkedService -ResourceGroupName $resourceGroupName -DataFactoryName $dataFactoryName -Name "AzureBatchLinkedService" -File "AzureBatchLinkedService.json"

set-AzureRmDataFactoryV2Pipeline -ResourceGroupName $resourceGroupName -DataFactoryName $dataFactoryName -Name $pipelineName -File "MyCustomActivityPipeline.json"

# Run pipeline and monitor output
$runId = Invoke-AzureRmDataFactoryV2Pipeline -DataFactoryName $dataFactoryName -ResourceGroupName $resourceGroupName -PipelineName $pipelineName
 
while ($True) {
    $result = Get-AzureRmDataFactoryV2ActivityRun -DataFactoryName $dataFactoryName -ResourceGroupName $resourceGroupName -PipelineRunId $runId -RunStartedAfter (Get-Date).AddMinutes(-30) -RunStartedBefore (Get-Date).AddMinutes(30)

    if (!$result) {
        Write-Host "Waiting for pipeline to start..." -foregroundcolor "Yellow"
    }
    elseif (($result | Where-Object { $_.Status -eq "InProgress" } | Measure-Object).count -ne 0) {
        Write-Host "Pipeline run status: In Progress" -foregroundcolor "Yellow"
    }
    else {
        Write-Host "Pipeline '"$pipelineName"' run finished. Result:" -foregroundcolor "Yellow"
        $result
        break
    }
    ($result | Format-List | Out-String)
    Start-Sleep -Seconds 15
}

Write-Host "Activity `Output` section:" -foregroundcolor "Yellow"
$result.Output -join "`r`n"

Write-Host "Activity `Error` section:" -foregroundcolor "Yellow"
$result.Error -join "`r`n"
