@echo off

echo "start main.cmd at %TIME%"

start /wait cmd /c "call install.cmd > %CUSTOM_SETUP_SCRIPT_LOG_DIR%\install.log"
REM error handling
if %ERRORLEVEL% neq 0 ( 
echo Failed with ExitCode %ERRORLEVEL%
exit /b %ERRORLEVEL%)
echo "Complete main.cmd at %TIME%"

