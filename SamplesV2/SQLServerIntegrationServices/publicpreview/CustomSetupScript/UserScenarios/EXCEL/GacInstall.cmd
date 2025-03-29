@echo off

powershell.exe -ExecutionPolicy RemoteSigned -File %~dp0\GacInstall.ps1 -AssemblyPath %1