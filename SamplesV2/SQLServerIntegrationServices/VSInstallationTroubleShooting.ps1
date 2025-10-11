
$logPath = "$env:TEMP\SsdtisSetup"
$logFile = Get-ChildItem -Path $logPath | Sort-Object Name -Descending | Select-Object -First 1

$content = Get-Content -Path $logFile.FullName
$errorDetected = $false


if ($logFile.Name.EndsWith("ISVsix.log")) { 
    foreach ($line in $content) {
        if ($line -match "AnotherInstallationRunning") {
            Write-Warning $line
            Write-Host "Another installation is currently in progress. Please wait until it completes before attempting to retry."
            Write-Host "More details: Windows Installer is preventing your installation. Windows Installer is a service of Windows that manages the installation of packages like MSIs, Windows Updates or third-party programs, and it can only run one installation at a time."
            $errorDetected = $true
            break
        }

        if ($line -match "Object reference not set to an instance") {
            Write-Warning $line
            Write-Host "There may be leftover caches from a previous installation. Please delete the corrupted instance folder: C:\ProgramData\Microsoft\VisualStudio\Packages\Instances\<InstallationID>."
            $errorDetected = $true
            break
        }

        if ($line -match "SoftRebootStatusCheck") {
            Write-Warning $line
            Write-Host "The soft reboot message means that a reboot is required before you can update or modify Visual Studio; however, you may continue using Visual Studio in the meantime."
            $errorDetected = $true
            break
        }
        
        if ($line -match "VSProcessesRunning") {
            Write-Warning $line
            Write-Host "Ensure that all Visual Studio-related processes (e.g., devenv.exe) are completely closed before starting the installation. Use Task Manager to end any lingering processes."
            $errorDetected = $true
            break
        }

        if ($line -match "Failed to verify hash of payload") {
            Write-Warning $line
            Write-Host " Delete the installation cache folder C:\ProgramData\Package Cache\15160B731819F56D87A626F9A2777550340022D7 then reinstall."
            $errorDetected = $true
            break
        }


        if ($line -match "System\.IO\.IOException:.*already exists" -and $line -match "[A-Z]:\\.*?Microsoft Visual Studio.*?\\Common7\\IDE") {
            $vsPath = $matches[0]
            Write-Host "`nPlease execute the following commands in an elevated command prompt:"
            cd $vsPath
            Write-Host 'Remove-Item "CommonExtensions\Microsoft\SSIS\*" -Recurse -Force'
            Write-Host 'Remove-Item "PublicAssemblies\SSIS\*" -Recurse -Force'
            Write-Host 'Remove-Item "PublicAssemblies\Microsoft BI\Business Intelligence Projects\Integration Services\*" -Recurse -Force'
            Write-Host "Repair Visual Studio via VS Installer, then you can reinstall SSIS"
            $errorDetected = $true
            break
        }
    }
}

if ($logFile.Name.EndsWith("ISVsixPreInstall.log")) { 
    foreach ($line in $content) {
        if ($line -match "UnauthorizedAccessException") {
            Write-Warning $line
            Write-Host "You might not have the necessary permissions to execute the installer. Please verify the folder permissions or consider running the installer as Administrator."
            $errorDetected = $true
        }
    }
}

if ($logFile.Name -match "VSTA2022.*\.log$") {
    Write-Host "Microsoft Visual Studio Tools for Applications 2022, required by SSIS, could not be installed due to errors."
    Write-Host "Install Microsoft Visual Studio Tools for Applications 2022 from https://www.microsoft.com/download/details.aspx?id=105123 then reinstall SSIS."
}

if (-not $errorDetected) {
    Write-Warning "Open $($logFile.Name) to see more detail first"
    Write-Host "If you can't resolve the errors, email $logFile to ssistoolsfeedbacks@microsoft.com for troubleshooting."
} else {
    Write-Host "See more details in $($logFile.Name)."
}

