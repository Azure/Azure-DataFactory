<#
.SYNOPSIS 
prepareOnPremDatabase&Table.ps1 creates sample database and table in your SQL Server.

.DESCRIPTION
Remember to update the information for your SQL Server (Server, User and Password) before running the script.
Please use SQL authentication.

.NOTES 
File Name : prepareOnPremDatabase&Table.ps1
Author    : Email adfsupport@microsoft.com for any help
Version   : 1.0
#>    

$dbServerName = "<servername>"
$dbUserName = "<username>"
$dbPassword = "<password>"

$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

# STEP 1 - Preparing the onPrem DB 
Write-Host  -foreground green (Get-Date) "Script to create sample on-premises SQL Server Database and Table"
Write-Host  -foreground green (Get-Date) "Creating the database [MarketingCampaigns], table and stored procedure on [$dbServerName]..."
Write-Host  -foreground green (Get-Date) "Connecting as user [$dbUserName] "

# Create the database, tables, and store procedure
sqlcmd -S $dbServerName -U $dbUserName -P $dbPassword -Q "create database MarketingCampaigns"
sqlcmd -S $dbServerName -U $dbUserName -P $dbPassword -d MarketingCampaigns -i (Join-Path $PSScriptRoot "..\Scripts\MarketingCampaignEffectiveness.sql")

Write-Host  -foreground green (Get-Date) "Summary:"
Write-Host  -foreground green (Get-Date) "1. Database 'MarketingCampaigns' created."
Write-Host  -foreground green (Get-Date) "2. 'MarketingCampaignEffectiveness' table and stored procedure created."
