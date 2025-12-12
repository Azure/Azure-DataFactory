@echo off

call GacInstall.cmd ExcelDataReader.dll
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully installation of ExcelDataReader.dll

call GacInstall.cmd DocumentFormat.OpenXml.dll
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo Successfully installation of DocumentFormat.OpenXml.dll
