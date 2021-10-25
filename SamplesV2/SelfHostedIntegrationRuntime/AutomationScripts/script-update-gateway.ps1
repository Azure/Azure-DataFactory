# This script is used to udpate/ install + register latest Microsoft Integration Runtime.
# And the steps are like this:
# 1. check current Microsoft Integration Runtime version
# 2. Get auto-update version or specified version from argument
# 3. if there is newer version than current version  
#    3.1 download Microsoft Integration Runtime msi
#    3.2 upgrade it

## And here is the usage:
## 1. Download and install latest Microsoft Integration Runtime
## PS > .\script-update-gateway.ps1
## 2. Download and install Microsoft Integration Runtime of specified version
## PS > .\script-update-gateway.ps1 -version 2.11.6380.20

param(
    [Parameter(Mandatory=$false)]
    [string]
    $version
)

function Get-CurrentGatewayVersion()
{
    $registryKeyValue = Get-RegistryKeyValue "Software\Microsoft\DataTransfer\DataManagementGateway\ConfigurationManager"

    $baseFolderPath = [System.IO.Path]::GetDirectoryName($registryKeyValue.GetValue("DiacmdPath"))
    $filePath = [System.IO.Path]::Combine($baseFolderPath, "Microsoft.DataTransfer.GatewayManagement.dll")
    
    $version = $null
    if (Test-Path $filePath)
    {
        $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($filePath).FileVersion
        $msg = "Current version: " + $version
        Write-Host $msg
    }
    
    return $version
}

function Get-LatestGatewayVersion()
{
    $latestGateway = Get-RedirectedUrl "https://go.microsoft.com/fwlink/?linkid=839822"
    $item = $latestGateway.split("/") | Select-Object -Last 1
    if ($item -eq $null -or $item -notlike "IntegrationRuntime*")
    {
        throw "Can't get latest Microsoft Integration Runtime info"
    }

    $regexp = '^IntegrationRuntime_(\d+\.\d+\.\d+\.\d+)\s*\.msi$'

    $version = [regex]::Match($item, $regexp).Groups[1].Value
    if ($version -eq $null)
    {
        throw "Can't get version from Microsoft Integration Runtime download uri"
    }

    $msg = "Auto-update version: " + $version
    Write-Host $msg
    return $version
}

function Get-RegistryKeyValue
{
     param($registryPath)

     $is64Bits = Is-64BitSystem
     if($is64Bits)
     {
          $baseKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
          return $baseKey.OpenSubKey($registryPath)
     }
     else
     {
          $baseKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry32)
          return $baseKey.OpenSubKey($registryPath)
     }
}


function Get-RedirectedUrl 
{
    $URL = "https://go.microsoft.com/fwlink/?linkid=839822"
 
    $request = [System.Net.WebRequest]::Create($url)
    $request.AllowAutoRedirect=$false
    $response=$request.GetResponse()
 
    If ($response.StatusCode -eq "Found")
    {
        $response.GetResponseHeader("Location")
    }
}

function Download-GatewayInstaller
{
    Param (
        [Parameter(Mandatory=$true)]
        [String]$version
    )

    Write-Host "Start to download MSI"
    $uri = Populate-Url $version
    $folder = New-TempDirectory
    $output = Join-Path $folder "IntegrationRuntime.msi"
    (New-Object System.Net.WebClient).DownloadFile($uri, $output)

    $exist = Test-Path($output)
    if ( $exist -eq $false)
    {
        throw "Cannot download specified MSI"
    }

    $msg = "New Microsoft Integration Runtime MSI has been downloaded to " + $output
    Write-Host $msg
    return $output
}

function Populate-Url
{
    Param (
        [Parameter(Mandatory=$true)]
        [String]$version
    )
    
    $uri = Get-RedirectedUrl
    $uri = $uri.Substring(0, $uri.LastIndexOf('/') + 1)
    $uri += "IntegrationRuntime_$version.msi"
    
    return $uri
}

function Install-Gateway
{
    Param (
        [Parameter(Mandatory=$true)]
        [String]$msi
    )

    $exist = Test-Path($msi)
    if ( $exist -eq $false)
    {
        throw 'there is no MSI found: $msi'
    }


    Write-Host "Start to install Microsoft Integration Runtime ..."

    $arg = "/i " + $msi + " /quiet /norestart"
    Start-Process -FilePath "msiexec.exe" -ArgumentList $arg -Wait -Passthru -NoNewWindow
    
    Write-Host "Microsoft Integration Runtime has been successfully updated!"
}

function New-TempDirectory {
    $parent = [System.IO.Path]::GetTempPath()
    [string] $name = [System.Guid]::NewGuid()
    New-Item -ItemType Directory -Path (Join-Path $parent $name)
}


function Is-64BitSystem
{
     $computerName= $env:COMPUTERNAME
     $osBit = (get-wmiobject win32_processor -computername $computerName).AddressWidth
     return $osBit -eq '64'
}

If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
    [Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Warning "You do not have Administrator rights to run this script!`nPlease re-run this script as an Administrator!"
    Break
}

$currentVersion = Get-CurrentGatewayVersion
if ($currentVersion -eq $null)
{
    Write-Host "There is no Microsoft Integration Runtime found on your machine, exiting ..."
    break
}

$versionToInstall = $version
if ([string]::IsNullOrEmpty($versionToInstall))
{
    $versionToInstall = Get-LatestGatewayVersion
}

if ([System.Version]$currentVersion -ge [System.Version]$versionToInstall)
{
    Write-Host "Your Microsoft Integration Runtime is latest, no update need..."
}
else
{
    $msi = Download-GatewayInstaller $versionToInstall
    Install-Gateway $msi
    Remove-Item -Path $msi -Force
}
