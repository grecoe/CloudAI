#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
#####################################################
param(
	[string]$subId,
	[switch]$login=$false,
	[switch]$help=$false
)


#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host "This script will look for the file deletegroups.txt created by the DelResourceGroups.ps1 file."
	Write-Host "That file contains a resource group name, per line, to verify it's existence. A subscription ID"
	Write-Host "Must be supplied."
	Write-Host "Parameters:"
	Write-Host "	-help : Shows this help message"
	Write-Host "	-login : Tells script to log into azure subscription, otherwise assumes logged in already"
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
	break
}

Set-StrictMode -Version 1


#####################################################
# Verify that subId is actually provided
#####################################################
if(-not $subId)
{
	Write-Host "-subId is a required parameter. Run the script with -help to get more information."
	break
}

#####################################################
# Log in and set to the sub you want to see
#####################################################
if($login -eq $true)
{
	Write-Host "Log into Azure...."
	Login-AzureRmAccount
}
else
{
	Write-Host "Bypassing Azure Login...."
}

Write-Host "Setting subscription ID : $subId"
Set-AzureRmContext -SubscriptionID $subId

$removedRG = New-Object System.Collections.ArrayList
$presentRG = New-Object System.Collections.ArrayList

foreach($resourceGroup in Get-Content .\deletegroups.txt) 
{
	$resourceGroup = $resourceGroup.Trim()
	$azureResource = Get-AzureRmResourceGroup -Name $resourceGroup -ErrorAction SilentlyContinue

	if ($azureResource)
	{
		# ResourceGroup doesn't exist
		$presentRG.Add($resourceGroup) > $null
	}
	else
	{
		# ResourceGroup exist
		$removedRG.Add($resourceGroup) > $null
	}
}


Write-Output "------------- REMOVED GROUPS ----------------"
foreach($grp in $removedRG)
{
	Write-Output "$grp"
}

Write-Output "------------- PRESENT GROUPS ----------------"
foreach($grp in $presentRG)
{
	Write-Output "$grp"
}
