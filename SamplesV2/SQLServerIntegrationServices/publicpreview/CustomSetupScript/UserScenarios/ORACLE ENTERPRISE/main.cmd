@echo off

echo "start main.cmd"
time /T

"%SystemRoot%\System32\msiexec.exe" /i AttunitySSISOraAdaptersSetup.msi /qn /norestart /log oracle32bit.log EulaForm_Property=Yes RestartSqlServerSilent=true
REM error handling
if %ERRORLEVEL% neq 0 (
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully installation of AttunitySSISOraAdaptersSetup.msi

"%SystemRoot%\System32\msiexec.exe" /i AttunitySSISOraAdaptersSetupX64.msi /qn /norestart /log oracle64bit.log EulaForm_Property=Yes RestartSqlServerSilent=true
REM error handling
if %ERRORLEVEL% neq 0 (
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully installation of AttunitySSISOraAdaptersSetupX64.msi

start /wait xcopy /R /F /Y ".\*.log" "%CUSTOM_SETUP_SCRIPT_LOG_DIR%\"

PowerShell Expand-Archive -Path "instantclient-basic-windows.x64-19.13.0.0.0dbru.zip" -DestinationPath "%SystemDrive%\OracleInstantClient"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully Expand-Archive instantclient-basic-windows.x64-19.13.0.0.0dbru.zip

setx /M PATH "%SystemDrive%\OracleInstantClient\instantclient_19_13\;%PATH%"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%) 
echo Successfully set environment path for Oracle instant client

reg add HKEY_LOCAL_MACHINE\SOFTWARE\ORACLE\ORACLE_19_13
reg add HKEY_LOCAL_MACHINE\SOFTWARE\ORACLE\ORACLE_19_13 /v ORACLE_HOME /t REG_SZ /d %SystemDrive%\OracleInstantClient\instantclient_19_13\
echo Successfully add registry for ORACLE_HOME

REM Set TNS_ADMIN variable for SSIS to read tbsnames.ora file
setx TNS_ADMIN "%SystemDrive%\OracleInstantClient\instantclient_19_13\network\admin" /M
REM error handling
if %ERRORLEVEL% neq 0 (
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully set TNS_ADMIN

REM Copy tnsnames.ora which contains the connection information to be used by SSIS package Oracle Connector
REM TNS Service name can also in format of host:port/service_name, which does not use tnsnames.ora
start /wait xcopy /R /F /Y %cd%\tnsnames.ora %SystemDrive%\OracleInstantClient\instantclient_19_13\network\admin\
REM error handling
if %ERRORLEVEL% neq 0 (
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully copied tnsnames.ora

echo "Target Log dir is %CUSTOM_SETUP_SCRIPT_LOG_DIR%"
dir "%CUSTOM_SETUP_SCRIPT_LOG_DIR%"

time /T
echo "Complete main.cmd"