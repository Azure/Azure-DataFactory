@echo off

echo Start to install AzurePowershell.msi

msiexec /i AzurePowershell.msi /quiet /lv %CUSTOM_SETUP_SCRIPT_LOG_DIR%\AzurePowershell.log
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully installation of AzurePowershell.msi
