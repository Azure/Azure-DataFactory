@echo off

echo Start to install MsSqlCmdLnUtils.msi
msiexec /i MsSqlCmdLnUtils.msi /quiet /lv %CUSTOM_SETUP_SCRIPT_LOG_DIR%\MsSqlCmdLnUtils.log IACCEPTMSSQLCMDLNUTILSLICENSETERMS=YES

REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%) 
echo Successfully installation of MsSqlCmdLnUtils.msi
