#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
# whatif - Run it as if we would delete but only display
#		   what would happen if executed.
#####################################################
param(
	[string]$subId,
	[switch]$help=$false,
	[switch]$login=$false,
	[switch]$delspecials=$false,
	[switch]$whatif=$false
)

#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host "This script will delete resource groups from an Azure Subscription."
	Write-Host "You will be prompted to log in but MUST provide a subscription ID."
	Write-Host "Parameters:"
	Write-Host "	-help : Shows this help message"
	Write-Host "	-whatif : Lists out resource groups that are locked and unlocked, unlocked resource groups will be deleted."
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-delspecials : Will delete even special resource groups EXCEPT cleanupservice."
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

Write-Host "Setting subscription ID : $subId"
Set-AzureRmContext -SubscriptionID $subId

#####################################################
# Display some text when it starts indicating what
# is going to happen. If no whatif, verify this is 
# what they want to do.
#####################################################
if($whatif -eq $true)
{
	Write-Host "This run will display what would happen, but nothing will be removed from the subscription."
	#Write-Host "Press any key to continue"
	#Read-Host
}
else
{
	Write-Host "This run will delete all unlocked resource groups in the subscription."
	$response = Read-Host 'Are you sure you want to delete all unlocked resource groups? (y/n) '
	if($response -ne "y")
	{
		Write-Host "Cancelling operations on this subscription"
		break
	}
}


#####################################################
# Collect all of the resource groups
#####################################################
Write-Host "Scanning subscription for resource groups, this could take a while....."

$totalResourceGroups = 0
$lockedRG = New-Object System.Collections.ArrayList
$readOnlyRG = New-Object System.Collections.ArrayList
$unlockedRG = New-Object System.Collections.ArrayList
$specialRG = New-Object System.Collections.ArrayList

$resourceGroups = Get-AzureRmResourceGroup #| Where-Object {$_.ResourceGroupName -eq "dangtestdel"}

#####################################################
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################
foreach($group in $resourceGroups)
{
	$totalResourceGroups++
	
	$name=$group.ResourceGroupName

	$locks = Get-AzureRmResourceLock -ResourceGroupName $name
	if($locks)
	{
		$foundlocks = New-Object System.Collections.ArrayList
		if($locks.Length -gt 0)
		{
			foreach($lock in $locks)
			{	
				$foundlocks.Add($lock) > $null
			}
		}
		else
		{
			$foundlocks.Add($locks) > $null
		}
	
		# It has a lock either ReadOnly or CanNotDelete so it has to 
		# be marked as locked.
		foreach($lock in $locks)
		{
			$properties = $lock.Properties | ConvertTo-Json
			$propobject = ConvertFrom-Json -InputObject $properties
			$lockType = $propobject.psobject.properties["level"].value
			if($lockType -like "CanNotDelete")
			{
				# add only if not already captured.
				if($lockedRG -notcontains $name)
				{
					$lockedRG.add($name) > $null
				}
				# if a delete lock is found, remove from the read only list
				if($readOnlyRG -contains $name)
				{
					$readOnlyRG.remove($name)
				}
			}
			if($lockType -like "ReadOnly")
			{
				# only add if not in the locked group
				if($lockedRG -notcontains $name)
				{
					$readOnlyRG.add($name) > $null
				}
			}
		}
	}
	else
	{
		# In all cases do not delete the default storage. 1 it's cheap and 
		# 2 it may contain VHD images that are leased blocking the deletion.
		if($name.Contains("cleanup") -or
		   $name.Contains("Default-Storage-") -or
		   ( ($delspecials -eq $false) -and
			  ($name.Contains("DefaultResourceGroup-") -or
				$name.Contains("Default-MachineLearning-") -or
				$name.Contains("cloud-shell-storage-") -or
				$name.Contains("Default-ServiceBus-") -or
				$name.Contains("Default-Web-") -or
				$name.Contains("OI-Default-") -or
				$name.Contains("Default-SQL") -or
				$name.Contains("StreamAnalytics-Default-"))
			)
		  )
		{
			$specialRG.Add($name) > $null
		}
		else
		{
			$unlockedRG.add($name) > $null
		}
	}

	if(($totalResourceGroups % 20) -eq 0)
	{
		Write-Host "Still scanning resource groups..."
	}
	
	if($totalResourceGroups -eq 50)
	{
		#break;
	}
}

#Create a directory
md -ErrorAction Ignore -Name $subId

#DELETE THIS LINE
md -ErrorAction Ignore -Name "unlockedstatus"
#DELETE THIS LINE

# Write out all unlocked resource groups for a verification later.
Write-Host "Writing out unlocked groups to file"
Out-File -FilePath .\$subId\deletegroups.txt -InputObject $unlockedRG

# Write out the status of the resource groups.
$resourceGroupStats = New-Object PSObject -Property @{ 
	Total = $totalResourceGroups; 
	DeleteLocked = $lockedRG.Count; 
	ReadOnlyLocked = $lockedRG.Count; 
	Unlocked = $unlockedRG.Count;
	Special = $specialRG.Count }
Out-File -FilePath .\$subId\rg_status.json -InputObject ($resourceGroupStats | ConvertTo-Json -depth 100)

# Write out the unlock configuration
$lockConfiguration = New-Object System.Collections.ArrayList
$lockedResourceGroups = New-Object System.Collections.ArrayList
foreach($grp in $readOnlyRG)
{
	$lockedResourceGroups.Add($grp) > $null
}
foreach($grp in $lockedRG)
{
	$lockedResourceGroups.Add($grp) > $null
}
$subscriptionLockConfiguration = New-Object PSObject -Property @{ 
	subscriptionId = $subId
	resourceGroups = $lockedResourceGroups
	}
$lockConfiguration.Add($subscriptionLockConfiguration) > $null
Out-File -FilePath .\$subId\unlockconfiguration.json -InputObject ($lockConfiguration | ConvertTo-Json -depth 100)

#DELETE THIS SECTION
$sharedConfiguration = New-Object PSObject -Property @{ 
	SubscriptionId = $subId
	UnlockedResourceGroups = $unlockedRG
	}
$unlockedFileName = ".\unlockedstatus\" + $subId + ".json"
Out-File -FilePath $unlockedFileName -InputObject ($sharedConfiguration | ConvertTo-Json -depth 100)
#DELETE THIS SECTION

#####################################################
# Output what we found if whatif is set, otherwise
# start the cleaning process.
#####################################################
Write-Output "Total Resource Groups : $totalResourceGroups, delete locked $($lockedRG.Count), readonly locked $($readOnlyRG.Count), unlocked : $($unlockedRG.Count), specials ignored $($specialRG.Count)"
if($whatif -eq $true)
{
	Write-Output "------------- SPECIAL GROUPS - IGNORED ----------------"
	foreach($grp in $specialRG)
	{
		Write-Output "Special: $grp"
	}

	Write-Output "------------- LOCKED GROUPS - READ ONLY LOCK ----------------"
	foreach($grp in $readOnlyRG)
	{
		Write-Output "Safe: $grp"
	}
	
	Write-Output "------------- LOCKED GROUPS - DELETE LOCK ----------------"
	foreach($grp in $lockedRG)
	{
		Write-Output "Safe : $grp"
	}

	Write-Output "------------- UNLOCKED GROUPS - TO BE DELETED ----------------"
	foreach($grp in $unlockedRG)
	{
		Write-Output "To Delete : $grp"
	}
}
else
{
	Write-Output "Starting to purge  $($unlockedRG.Count) unlocked resource groups."

	foreach($grp in $unlockedRG)
	{
		Write-Output "Deleting : $grp"
		Remove-AzureRmResourceGroup -Force -Name $grp
	}
}