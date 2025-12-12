@echo off

powershell.exe -ExecutionPolicy RemoteSigned -File %~dp0\InstallNetFx35.ps1

REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%) 
echo Successfully installation of .net framwork 3.5
