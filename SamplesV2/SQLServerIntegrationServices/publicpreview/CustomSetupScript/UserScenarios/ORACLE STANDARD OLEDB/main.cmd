@echo off

PowerShell Expand-Archive -Path ".\ODAC122010Xcopy_x64.zip" -DestinationPath ".\ODAC122010Xcopy_x64"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully Expand-Archive ODAC122010Xcopy_x64.zip completed

start /D .\ODAC122010Xcopy_x64 /wait cmd /c "call install.bat oledb %SystemDrive%\ODAC odbc > %CUSTOM_SETUP_SCRIPT_LOG_DIR%\install2.log"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully Execute install.bat completed

REM need to set install path to environment variable
setx /M PATH "%SystemDrive%\ODAC;%SystemDrive%\ODAC\bin;%PATH%"

REM install.bat will redirect some of the standard output to %SystemDrive%\ODAC\install.log,
REM we need to copy it to %CUSTOM_SETUP_SCRIPT_LOG_DIR% so that it can be uploaded to your blob container
start /wait xcopy /R /F /Y %SystemDrive%\ODAC\install.log %CUSTOM_SETUP_SCRIPT_LOG_DIR%
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully Copied %SystemDrive%\ODAC\install.log to %CUSTOM_SETUP_SCRIPT_LOG_DIR%
