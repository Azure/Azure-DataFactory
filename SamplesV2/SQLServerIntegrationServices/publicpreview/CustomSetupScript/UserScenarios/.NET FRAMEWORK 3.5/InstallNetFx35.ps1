Try {
	if ((Get-WindowsFeature -Name Net-Framework-Core).Installed)
	{
		Write-Output ".NET framework 3.5 has already been installed."
	}
	else
	{
		if ((Install-WindowsFeature -Name Net-Framework-Core -Source (Get-Location).Path -LogPath %CUSTOM_SETUP_SCRIPT_LOG_DIR%\install.log).Success)
		{
			Write-Output ".NET framework 3.5 has been installed successfully"
		}
		else
		{
			throw "Failed to install .NET framework 3.5"
		}
	}
}
Catch {
	Exit 1
}
