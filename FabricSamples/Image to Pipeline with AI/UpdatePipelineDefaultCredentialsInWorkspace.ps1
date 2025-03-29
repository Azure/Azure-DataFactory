param(
    [Parameter(Mandatory=$True)]
    [string]
    $WorkspaceId,
    [Parameter(Mandatory=$True)]
    [string]
    $PipelineIds,
	[Parameter(Mandatory=$True)]
    [string]
    $token
 )

$pipelineIdsSunArray = $PipelineIds.Split(",")
$fabricEndpoint = "https://api.fabric.microsoft.com"

function LogMessage($message)
{
    Write-Host "`n$message"
}

function GetPipeline($workspaceId, $pipelineId) {
    LogMessage "Getting Pipeline $pipelineName"

    # Get-Item For Pipeline Artifact (https://learn.microsoft.com/en-us/rest/api/fabric/core/items/get-item?tabs=HTTP)
    $getPipelineArtifactMetadata = Invoke-RestMethod -URI "$fabricEndpoint/v1/workspaces/$workspaceId/items/$pipelineId" -Method GET -Headers @{Authorization="Bearer $token"}

    LogMessage "Pipeline Artifact Metadata: $getPipelineArtifactMetadata"
    return $getPipelineArtifactMetadata
}

function UpdatePipeline($workspaceId, $pipelineId, $displayName, $description) {
    LogMessage "Updating pipelines"
    
    $updatePipelineRequest = @"
    {
        "displayName": "$displayName", 
        "description": "$description - $((Get-Date).ToString())"
    }
"@

    # Update-Item for Pipeline Artifact (https://learn.microsoft.com/en-us/rest/api/fabric/core/items/update-item?tabs=HTTP)
    $updatedPipelineArtifactMetadata = Invoke-RestMethod -URI "$fabricEndpoint/v1/workspaces/$workspaceId/items/$pipelineId" -Method PATCH -Headers @{Authorization="Bearer $token"} -body $updatePipelineRequest -ContentType "application/json"

    LogMessage "Update Pipeline Artifact Metadata: $updatedPipelineArtifactMetadata"
}

LogMessage "Start: Updating pipeline descriptions"

ForEach($PipelineId in $pipelineIdsSunArray)
{
    LogMessage "PipelineId: $PipelineId"
    LogMessage "workspace: $WorkspaceId"

    $getPipelineArtifactMetadata = GetPipeline $WorkspaceId $pipelineId
    UpdatePipeline $WorkspaceId $pipelineId $getPipelineArtifactMetadata.displayName $getPipelineArtifactMetadata.description
}

LogMessage "Stop: Pipeline descriptions updated successfully"
LogMessage "Thank you!"
