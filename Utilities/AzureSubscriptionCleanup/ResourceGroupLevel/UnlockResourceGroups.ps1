#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Optional - Switch to show usage
# login - Optional - Switch if present requires login
#
# Removes locks from every resource group.
#####################################################
param(
	[string]$subId,
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
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################
Write-Host "Setting subscription ID : " + $subId
Set-AzureRmContext -SubscriptionID $subId

$resourceGroups = Get-AzureRmResourceGroup 

foreach($rg in $resourceGroups)
{
	$locks = Get-AzureRmResourceLock -ResourceGroupName $rg.ResourceGroupName

	if($locks.length -ne 0)
	{
		# It has a lock either ReadOnly or CanNotDelete so it has to 
		# be marked as locked.
		foreach($lock in $locks)
		{
			Write-Host("Removing lock : " + $lock.LockId + " from " + $rg.ResourceGroupName)
			Write-Host("--------------------------------------------------------")
			
			Remove-AzureRmResourceLock -Force -LockId $lock.LockId
		}
	}
}

