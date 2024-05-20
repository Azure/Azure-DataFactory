@echo off

echo "start install.cmd at %TIME%"

msiexec /i AttunitySSISTeraAdaptersSetup.msi /quiet /qn /norestart /log %CUSTOM_SETUP_SCRIPT_LOG_DIR%/install_32bitconnector.log AdminEulaForm_Property=Yes RestartSqlServerSilent=true

REM Above install command will return immediately
REM Install takes about 10 seconds, sleep 20 seconds to make sure installation complete
timeout 20  >nul 2>&1

msiexec /i AttunitySSISTeraAdaptersSetupX64.msi /quiet /qn /norestart /log %CUSTOM_SETUP_SCRIPT_LOG_DIR%/install_64bitconnector.log AdminEulaForm_Property=Yes RestartSqlServerSilent=true

REM Above install command will return immediately
REM Install takes about 14 seconds, sleep 30 seconds to make sure installation complete
timeout 30  >nul 2>&1

echo "Connector install complete, start to unzip Teradata Tools and Utilities package at %TIME%"
PowerShell Expand-Archive -Path ".\TeradataToolsAndUtilitiesBase__windows_indep.15.10.22.00.zip" -DestinationPath ".\TeradataToolsAndUtilitiesBase__windows_indep.15.10.22.00"

echo "Start Install TPT API at %TIME%"
start /wait cmd /c call %cd%\TeradataToolsAndUtilitiesBase__windows_indep.15.10.22.00\TeradataToolsAndUtilitiesBase\Windows\TTU\silent_install.bat "TPTBase,TPTStream"

echo "TPT API installation complete, start to copy log at %TIME%"
xcopy /R /F /Y "%temp%\SharedICU*" %CUSTOM_SETUP_SCRIPT_LOG_DIR%
xcopy /R /F /Y "%temp%\TeraGSS*" %CUSTOM_SETUP_SCRIPT_LOG_DIR%
xcopy /R /F /Y "%temp%\ODBC*" %CUSTOM_SETUP_SCRIPT_LOG_DIR%
xcopy /R /F /Y "%temp%\CLIv2*" %CUSTOM_SETUP_SCRIPT_LOG_DIR%
xcopy /R /F /Y "%temp%\DataConnector*" %CUSTOM_SETUP_SCRIPT_LOG_DIR%
xcopy /R /F /Y "%temp%\TPTBase*" %CUSTOM_SETUP_SCRIPT_LOG_DIR%
xcopy /R /F /Y "%temp%\TPTStream*" %CUSTOM_SETUP_SCRIPT_LOG_DIR%
xcopy /R /F /Y "%temp%\TTUSilentInstall*" %CUSTOM_SETUP_SCRIPT_LOG_DIR%
xcopy /R /F /Y "%temp%\TTUSuiteSilent_Install*" %CUSTOM_SETUP_SCRIPT_LOG_DIR%

echo "Complete install.cmd at %TIME%"