param (
    [Parameter(Mandatory = $false)]
    [string]
    $version,
    [Parameter(Mandatory = $false)]
    [string]
    $allowDowngrade,
    [Parameter(Mandatory = $false)]
    [string]
    $servicePassword
)

$ErrorActionPreference = "Stop"

$ProductName = "Microsoft Integration Runtime"
$supportedVersion = [System.Version]::new("5.4.7793.1")

function Get-PushedIntegrationRuntimeVersion() {
    $latestIR = Get-RedirectedUrl "https://go.microsoft.com/fwlink/?linkid=839822"
    $item = $latestIR.split("/") | Select-Object -Last 1
    if ($null -eq $item -or $item -notlike "IntegrationRuntime*") {
        throw "Can't get pushed $ProductName info"
    }

    $regexp = '^IntegrationRuntime_(\d+\.\d+\.\d+\.\d+)\s*\.msi$'

    $version = [regex]::Match($item, $regexp).Groups[1].Value
    if ($null -eq $version) {
        throw "Can't get version from $ProductName download uri"
    }

    Write-InfoMsg "Pushed $ProductName version is $version"

    return $version
}

function Get-IntegrationRuntimeInstaller([string] $folder, [string] $version) {
    $uri = Get-InstallerUrl $version
    $output = Join-Path $folder "IntegrationRuntime.msi"
    Write-InfoMsg "Start to download $ProductName installer of version $version from $uri"
    (New-Object System.Net.WebClient).DownloadFile($uri, $output)

    if (-Not (Test-Path $output -PathType Leaf)) {
        throw "Cannot download $ProductName installer of version $version"
    }

    Write-InfoMsg "$ProductName installer has been downloaded to $output."
    return $output
}

function Get-InstallerUrl([string] $version) {
    $uri = Get-RedirectedUrl
    $uri = $uri.Substring(0, $uri.LastIndexOf('/') + 1)
    $uri += "IntegrationRuntime_$version.msi"

    return $uri
}

function Get-RedirectedUrl {
    $URL = "https://go.microsoft.com/fwlink/?linkid=839822"

    $request = [System.Net.WebRequest]::Create($url)
    $request.AllowAutoRedirect = $false
    $response = $request.GetResponse()

    If ($response.StatusCode -eq "Found") {
        $response.GetResponseHeader("Location")
    }
}

function New-TempDirectory {
    $parent = [System.IO.Path]::GetTempPath()
    [string] $name = [System.Guid]::NewGuid()
    return (New-Item -ItemType Directory -Path (Join-Path $parent $name))
}

function Get-CurrentIntegrationRuntimeVersion() {
    $baseFolderPath = [System.IO.Path]::GetDirectoryName((Get-CmdFilePath))
    $filePath = [System.IO.Path]::Combine($baseFolderPath, "Microsoft.DataTransfer.GatewayManagement.dll")
    
    $currentVersion = $null
    if (Test-Path $filePath -PathType Leaf) {
        $currentVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($filePath).FileVersion
        Write-InfoMsg "Current $ProductName version is $currentVersion"
    }

    return $currentVersion
}

function Get-RegistryKeyValue ([string] $registryPath) {
    $baseKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
    return $baseKey.OpenSubKey($registryPath)
}

function Get-IntegrationRuntimeIdentityNumber {
    $installedSoftwares = Get-ChildItem "hklm:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
    foreach ($installedSoftware in $installedSoftwares) {
        $displayName = $installedSoftware.GetValue("DisplayName")
        if ($DisplayName -eq "$ProductName Preview" -or $DisplayName -eq "$ProductName") {
            return Split-Path $installedSoftware.Name -Leaf
        }
    }

    return $null
}

function Backup-IntegrationRuntimeConfig ([string] $installPath) {
    Write-InfoMsg "Start to backup $ProductName configuration files."
    $cmd = Get-CmdFilePath
    $process = Start-Process $cmd "-ucf ""$installPath"" store" -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0) {
        throw "Failed to backup $ProductName configuration files. Exit code: $($process.ExitCode)"
    }
    Write-InfoMsg "Succeed to backup $ProductName configuration files."
}

function Restore-IntegrationRuntimeConfig ([string] $installPath) {
    Write-InfoMsg "Start to restore $ProductName configuration files."
    $cmd = Get-CmdFilePath
    $process = Start-Process $cmd "-ucf ""$installPath"" recover true" -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0) {
        throw "Failed to restore $ProductName configuration files. Exit code: $($process.ExitCode)"
    }
    Write-InfoMsg "Succeed to restore $ProductName configuration files."
}

function Get-CmdFilePath {
    $registryKeyValue = Get-RegistryKeyValue "Software\Microsoft\DataTransfer\DataManagementGateway\ConfigurationManager"
    $filePath = $registryKeyValue.GetValue("DiacmdPath")
    if ([string]::IsNullOrEmpty($filePath)) {
        throw "Cannot find CLI executable file."
    }
    return (Split-Path -Parent $filePath) + "\dmgcmd.exe"
}

function Get-IntegrationRuntimeServiceAccount {
    $irService = Get-WmiObject win32_service | Where-Object { $_.Name -eq "DIAHostService" }
    return $irService.startname
}

function Set-IntegrationRuntimeServiceAccount ([string] $account, [string] $password) {
    $cmd = Get-CmdFilePath
    $process = Start-Process $cmd "-ssa $account $password" -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0) {
        throw "Failed to set service account of $ProductName. Exit code: $($process.ExitCode)"
    }
    Write-InfoMsg "Succeed to set service account of $ProductName."
}

function Get-IntegrationRuntimeExeFolder {
    $registryKeyValue = Get-RegistryKeyValue "Software\Microsoft\DataTransfer\DataManagementGateway\ConfigurationManager"
    return Split-Path $registryKeyValue.GetValue("DiacmdPath") -Parent
}

function Uninstall-IntegrationRuntime ([string] $identityNumber) {
    Write-InfoMsg "Start to uninstall $ProductName with identity number: $identityNumber"
    $process = Start-Process "msiexec.exe" "/x $identityNumber /quiet KEEPDATA=1" -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0) {
        throw "Failed to uninstall $ProductName. Exit code: $($process.ExitCode). Please try to uninstall $ProductName manually"
    }
    Write-InfoMsg "Succeed to uninstall $ProductName."
}

function Install-IntegrationRuntime ([string] $installerPath, [string] $installedPath, [boolean] $skipStartService) {
    Write-InfoMsg "Start to install $ProductName"
    $installArgs = "/i $installerPath /quiet"
    if (-not [string]::IsNullOrEmpty($installedPath)) {
        $installArgs += " INSTALLLOCATION=""$installedPath"""
    }
    if ($skipStartService) {
        $installArgs += " SKIPSTARTSERVICE=""Yes"""
    }
    Write-InfoMsg "Install arguments: $installArgs"
    $process = Start-Process "msiexec.exe" $installArgs -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0) {
        throw "Failed to install $ProductName. Exit code: $($process.ExitCode). Please try to install $installPath manually"
    }
    Write-InfoMsg "Succeed to install $ProductName."
}

function Write-InfoMsg([string] $msg) {
    Write-Host "[$(Get-UtcNowString)][Info] $msg"
}

function Write-ErrorMsg([string] $msg) {
    Write-Host "[$(Get-UtcNowString)][Error] $msg" -ForegroundColor Red
}

function Get-UtcNowString {
    $utcNow = [System.TimeZoneInfo]::ConvertTimeBySystemTimeZoneId((Get-Date), 'UTC')
    return $utcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
}

# Main
If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
            [Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-ErrorMsg "You do not have Administrator rights to run this script!`nPlease re-run this script as an Administrator!"
    Exit 1001
}

if ([string]::IsNullOrWhiteSpace($version)) {
    $version = Get-PushedIntegrationRuntimeVersion
}

$versionObject = $null
if (-Not [System.Version]::TryParse($version, [ref]$versionObject)) {
    Write-ErrorMsg "Invalid version value: $version."
    Exit 1002
}
if ($versionObject -lt $supportedVersion) {
    Write-ErrorMsg "Update to version less than $supportedVersion isn't supported."
    Exit 1003
}
Write-InfoMsg "The version to be installed is $version"

$identityNumber = Get-IntegrationRuntimeIdentityNumber
$installed = ($null -ne $identityNumber)

if ($installed -and ($version -ieq (Get-CurrentIntegrationRuntimeVersion))) {
    Write-InfoMsg "Current installed $ProductName is $version, no operation needed."
    Exit
}
$isDowngrade = ($installed -and ($versionObject -lt [System.Version]::Parse((Get-CurrentIntegrationRuntimeVersion))))

if ($isDowngrade -and ($allowDowngrade -ine "true")) {
    Write-ErrorMsg "You're trying to downgrade $ProductName, please set '-allowDowngrade' to true to enable downgrade."
    Exit 1004
}

$serviceAccount = $null
if ($installed -and ("NT SERVICE\DIAHostService" -ine (Get-IntegrationRuntimeServiceAccount))) {
    $serviceAccount = Get-IntegrationRuntimeServiceAccount
    Write-InfoMsg "$ProductName service account is $serviceAccount"
}
if (($null -ne $serviceAccount) -and $isDowngrade -and ($null -eq $servicePassword)) {
    Write-ErrorMsg "$ProductName isn't run as default account. You need to provide password of $serviceAccount with option '-servicePassword' to process downgrade."
    Exit 1005
}

$installedPath = $null
if ($installed) {
    $installedPath = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Get-CmdFilePath))))
}

$tmpFolder = New-TempDirectory

$installerPath = Get-IntegrationRuntimeInstaller $tmpFolder $version
if (-Not (Test-Path -Path $installerPath -PathType Leaf)) {
    Write-ErrorMsg "The installer $installerPath doesn't exist."
    Exit
}

if ($installed -and $isDowngrade) {
    $backupPath = Join-Path -Path $installedPath -ChildPath "$([System.Version]::Parse((Get-CurrentIntegrationRuntimeVersion)).Major).0"
    Backup-IntegrationRuntimeConfig $backupPath
    Write-InfoMsg "Uninstall old $ProductName."
    Uninstall-IntegrationRuntime $identityNumber
}

Write-InfoMsg "Install $ProductName"
Install-IntegrationRuntime $installerPath $installedPath ($null -ne $serviceAccount)

if ($isDowngrade) {
    try {
        $restorePath = Join-Path -Path $installedPath -ChildPath "$($versionObject.Major).0"
        Restore-IntegrationRuntimeConfig $restorePath
    }
    catch {
        Write-ErrorMsg $_
    }
    if ($null -ne $serviceAccount) {
        try {
            Set-IntegrationRuntimeServiceAccount $serviceAccount $servicePassword
        }
        catch {
            Write-ErrorMsg $_
        }
    }
}

Write-InfoMsg "Clean up downloaded $ProductName installer: $installerPath."
Remove-Item $installerPath

if ($null -eq $identityNumber) {
    Write-InfoMsg "Install complete. You may need to open $ProductName to manually register node."
}
