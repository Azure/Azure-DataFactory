@echo off

xcopy /F /Y SleepTask.dll "%ProgramFiles%\Microsoft SQL Server\140\DTS\Tasks"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo "Complete copied to x64 folder"

xcopy /F /Y SleepTask.dll "%ProgramFiles(x86)%\Microsoft SQL Server\140\DTS\Tasks"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo "Complete copied to x86 folder"

gacutil\gacutil /i SleepTask.dll /f
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully installed Sleep Task.

REM If you want to persist access credentials for file shares or Azure Files, use the commands below:
REM cmdkey /add:YourFileShareServerName /user:YourDomainName\YourUsername /pass:YourPassword
REM cmdkey /add:YourAzureStorageAccountName.file.core.windows.net /user:azure\YourAzureStorageAccountName /pass:YourAccessKey
REM You can then access \\YourFileShareServerName\YourFolderName or \\YourAzureStorageAccountName.file.core.windows.net\YourFolderName directly in your SSIS packages.