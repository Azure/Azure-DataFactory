@echo off

REM Copy for SAP connector depended file
start /wait xcopy /R /F /Y librfc32.dll %windir%\System32\
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
start /wait xcopy /R /F /Y librfc32.dll %windir%\SysWow64\
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully copied for SAP connector depended file
