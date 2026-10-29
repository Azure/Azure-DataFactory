@echo off

echo Installing Zulu OpenJDK...

powershell.exe -file install_openjdk.ps1

setx /M _JAVA_OPTIONS "-Xms256m -Xmx16g"

echo Installation completed