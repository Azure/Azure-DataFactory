param(
    [Parameter(Mandatory=$true)]
    [string]
    $path, 
    [Parameter(Mandatory=$true)]
    [string]
    $authKey,
    [Alias("port")]
    [Parameter(Mandatory=$false)]
    [string]
    $remoteAccessPort,
    [Alias("cert")]
    [Parameter(Mandatory=$false)]
    [string]
    $remoteAccessCertThumbprint
)
function Install-Gateway([string] $gwPath)
{
    # uninstall any existing gateway
    UnInstall-Gateway

    Write-Host "Start Microsoft Integration Runtime installation"
    
    $process = Start-Process "msiexec.exe" "/i $path /quiet /passive" -Wait -PassThru
    if ($process.ExitCode -ne 0)
    {
        throw "Failed to install Microsoft Integration Runtime. msiexec exit code: $($process.ExitCode)"
    }
    Start-Sleep -Seconds 30	

    Write-Host "Succeed to install Microsoft Integration Runtime"
}

function Register-Gateway([string] $key, [string] $port, [string] $cert)
{
    $cmd = Get-CmdFilePath

    if (![string]::IsNullOrEmpty($port))
    {
        Write-Host "Start to enable remote access."
        $process = Start-Process $cmd "-era $port $cert" -Wait -PassThru -NoNewWindow
        if ($process.ExitCode -ne 0)
        {
            throw "Failed to enable remote access. Exit code: $($process.ExitCode)"
        }
        Write-Host "Succeed to enable remote access."
    }

    Write-Host "Start to register Microsoft Integration Runtime with key: $key."
    $process = Start-Process $cmd "-k $key" -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0)
    {
        throw "Failed to register Microsoft Integration Runtime. Exit code: $($process.ExitCode)"
    }
    Write-Host "Succeed to register Microsoft Integration Runtime."
}

function Check-WhetherGatewayInstalled([string]$name)
{
    $installedSoftwares = Get-ChildItem "hklm:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
    foreach ($installedSoftware in $installedSoftwares)
    {
        $displayName = $installedSoftware.GetValue("DisplayName")
        if($DisplayName -eq "$name Preview" -or  $DisplayName -eq "$name")
        {
            return $true
        }
    }

    return $false
}


function UnInstall-Gateway()
{
    $installed = $false
    if (Check-WhetherGatewayInstalled("Microsoft Integration Runtime"))
    {
        [void](Get-WmiObject -Class Win32_Product -Filter "Name='Microsoft Integration Runtime Preview' or Name='Microsoft Integration Runtime'" -ComputerName $env:COMPUTERNAME).Uninstall()
        $installed = $true
    }

    if (Check-WhetherGatewayInstalled("Microsoft Integration Runtime"))
    {
        [void](Get-WmiObject -Class Win32_Product -Filter "Name='Microsoft Integration Runtime Preview' or Name='Microsoft Integration Runtime'" -ComputerName $env:COMPUTERNAME).Uninstall()
        $installed = $true
    }

    if ($installed -eq $false)
    {
        Write-Host "Microsoft Integration Runtime is not installed."
        return
    }

    Write-Host "Microsoft Integration Runtime has been uninstalled from this machine."
}

function Get-CmdFilePath()
{
    $filePath = Get-ItemPropertyValue "hklm:\Software\Microsoft\DataTransfer\DataManagementGateway\ConfigurationManager" "DiacmdPath"
    if ([string]::IsNullOrEmpty($filePath))
    {
        throw "Get-InstalledFilePath: Cannot find installed File Path"
    }

    return (Split-Path -Parent $filePath) + "\dmgcmd.exe"
}

function Validate-Input([string]$path, [string]$key)
{
    if ([string]::IsNullOrEmpty($path))
    {
        throw "Microsoft Integration Runtime path is not specified"
    }

    if (!(Test-Path -Path $path))
    {
        throw "Invalid Microsoft Integration Runtime path: $path"
    }

    if ([string]::IsNullOrEmpty($key))
    {
        throw "Microsoft Integration Runtime Auth key is empty"
    }
}

If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(`
    [Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Warning "You do not have Administrator rights to run this script!`nPlease re-run this script as an Administrator!"
    Break
}

Validate-Input $path $authKey

Install-Gateway $path
Register-Gateway $authKey $remoteAccessPort $remoteAccessCertThumbprint