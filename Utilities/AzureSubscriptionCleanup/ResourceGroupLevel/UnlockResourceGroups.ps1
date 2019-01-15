#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# path - Required - path to input file
# help - Optional - Switch to show usage
# login - Optional - Switch if present requires login
#
# Input File Format:
# JSON Array of objects identifying subscriptions and resource group names 
# to unlock.
#	[
#		{
#			"subscriptionId" : "your sub id",
#			"resourceGroups" : [ "list of group names"]
#		}
#	]
#####################################################
param(
	[string]$subId,
	[string]$path,
	[switch]$help=$false,
	[switch]$login=$false
)

#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host "This script will remove locks from resource groups in an Azure Subscription."
	Write-Host "You will be prompted to log in but MUST provide a subscription ID."
	Write-Host "Parameters:"
	Write-Host "	-help : Shows this help message"
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-path : Full disk path of file containing subscription and resouce group name information."
	Write-Host "	-login : Tells script to log into azure subscription, otherwise assumes logged in already"
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
# Verify that a file is actually provided
#####################################################
if(-not $path)
{
	Write-Host "-path is a required parameter. Run the script with -help to get more information."
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


#####################################################
# Collect all of the resource groups
#####################################################
Write-Host("Loading information from file : " + $path)
$unlockData = Get-Content -Raw -Path $path | ConvertFrom-Json


#####################################################
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################
foreach($sub in $unlockData)
{
	Write-Host "Setting subscription ID : " + $sub.subscriptionId
	Set-AzureRmContext -SubscriptionID $sub.subscriptionId

	foreach($rg in $sub.resourceGroups)
	{
		$locks = Get-AzureRmResourceLock -ResourceGroupName $rg
	
		if($locks.length -ne 0)
		{
			# It has a lock either ReadOnly or CanNotDelete so it has to 
			# be marked as locked.
			foreach($lock in $locks)
			{
				Write-Host("Removing lock : " + $lock.LockId)
				Write-Host("--------------------------------------------------------")
			
				Remove-AzureRmResourceLock -Force -LockId $lock.LockId
			}
		}
	}
}

