@echo off

PowerShell Expand-Archive -Path ".\ODP.NET_Managed_ODAC122cR1.zip" -DestinationPath ".\ODP.NET"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully Expand-Archive ODP.NET_Managed_ODAC122cR1.zip 

start /D .\ODP.NET /wait cmd /c "call install_odpm.bat %SystemDrive%\ODP.NET both true > %CUSTOM_SETUP_SCRIPT_LOG_DIR%\install2.log"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully Execute install_odpm.bat

REM install_odpm.bat will redirect some of the standard output to %SystemDrive%\ODP.NET\install.log,
REM we need to copy it to %CUSTOM_SETUP_SCRIPT_LOG_DIR% so that it can be uploaded to your blob container

start /wait xcopy /R /F /Y %SystemDrive%\ODP.NET\install.log %CUSTOM_SETUP_SCRIPT_LOG_DIR%
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully Copied %SystemDrive%\ODP.NET\install.log to %CUSTOM_SETUP_SCRIPT_LOG_DIR%

