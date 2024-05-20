@echo off

set DriverFolderName=instantclient_18_3
set DriverName=Oracle in instantclient_18_3

REM Please overwrite the value of following three variables
set DSN=<DSN Name>
set ServerName=<Oracle Server Name>
set UserID=<User ID>

echo Install Oracle ODBC Driver ...
powershell.exe Expand-Archive -Path ".\instantclient-basiclite-windows.x64-18.3.0.0.0dbru.zip" -DestinationPath "%SystemDrive%\oracle"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully Expand-Archive instantclient-basiclite-windows.x64

powershell.exe Expand-Archive -Path ".\instantclient-odbc-windows.x64-18.3.0.0.0dbru.zip" -DestinationPath "%SystemDrive%\oracle"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully Expand-Archive instantclient-odbc-windows.x64

start /D %SystemDrive%\oracle\%DriverFolderName% .\odbc_install.exe
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully installed Oracle ODBC Driver!

setx /M PATH "%SystemDrive%\oracle\%DriverFolderName%;%PATH%"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%) 
echo Successfully set environment path

echo Add DSN %DSN%
odbcconf CONFIGSYSDSN "%DriverName%" "DSN=%DSN%|SERVER=%ServerName%|UID=%UserID%" /S /Lv %CUSTOM_SETUP_SCRIPT_LOG_DIR%\odbcconf.txt

REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully added DSN %DSN%!
