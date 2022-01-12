Param(
    [string] $StorageAccountName,
    [string] $RepoURL)

Connect-AzAccount -Identity

$FileList = "filelist.txt"
$FileListUri = "$RepoURL/infra/$FileList"
Write-Host "Download file list from: $FileListUri"

Invoke-WebRequest -Uri $FileListUri -OutFile $FileList

$Context = New-AzStorageContext -UseConnectedAccount -StorageAccountName $StorageAccountName

# upload test content to storage account
foreach($file in Get-Content $FileList) {
    Invoke-WebRequest -Uri $RepoURL/content/$file -OutFile $file
    
    $Blob = @{
        File             = $file
        Container        = "raw"
        Blob             = $file
        Context          = $Context
        StandardBlobTier = 'Hot'
    }
    Set-AzStorageBlobContent -Force @Blob
}

# upload batch code to storage account
$Src = "src.txt"
$SrcUri = "$RepoURL/infra/$Src"
Write-Host "Download src files from: $SrcUri"
Invoke-WebRequest -Uri $SrcUri -OutFile $Src
foreach($file in Get-Content $Src) {
    Invoke-WebRequest -Uri $RepoURL/src/$file -OutFile $file
    
    $Blob = @{
        File             = $file
        Container        = "azure-batch"
        Blob             = $file
        Context          = $Context
        StandardBlobTier = 'Hot'
    }
    Set-AzStorageBlobContent -Force @Blob
}