param(
    [string]$AssemblyPath
)

$AssemblyPath = [System.IO.Path]::GetFullPath($AssemblyPath)
Write-Output "Start to Gac assembly '$AssemblyPath'"

$assembly = [System.Reflection.Assembly]::LoadFile($AssemblyPath)
if ($assembly.GetName().GetPublicKey().Length -eq 0)
{
    throw "The assembly '$assembly' is not strong name signed!"
}
elseif ($assembly.GlobalAssemblyCache)
{
    Write-Output "The assembly '$assembly' has already been GACed."
    exit 0
}

[System.Reflection.Assembly]::LoadWithPartialName("System.EnterpriseServices") | Out-Null
$publish = New-Object System.EnterpriseServices.Internal.Publish
$publish.GacInstall($AssemblyPath)

$assembly = [System.Reflection.Assembly]::LoadFile($AssemblyPath)
if ($assembly.GlobalAssemblyCache)
{
    Write-Output "Successfully GACed assembly '$assembly'."
}
else
{
    throw "Failed to Gac assembly '$assembly'."
}


