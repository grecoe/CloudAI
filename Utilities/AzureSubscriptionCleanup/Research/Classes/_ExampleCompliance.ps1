#############################################################################
#	In this example compliance, we look at ONLY unlocked resource groups.
#	If the group does not contain ALL of the following tags, it is deleted:
#	
#	Tags: alias, project, expires
#############################################################################

. './clsSubscription.ps1'
. './clsResourceGroupManager.ps1'

# Perform a login prior to calling this, first call collects the subscriptions.
$subManager = [SubscriptionManager]::new()
$currentSubscription = $null
$expectedTags = @('alias', 'project', 'expires')

# Filter on subscriptions by a name or partial name 
$subscriptionNameToFind="DevOps"
Write-Host("Searching for:  " + $subscriptionNameToFind )
$result = $subManager.FindSubscription($subscriptionNameToFind)

# Possible to get more than one result, so....be careful.
if($result.Count -eq 1)
{
	$currentSubscription = $result[0]
	
	Write-Host("Working with subscription " + $currentSubscription.Name)
	
	# Set this subscription as the current subscription to work on.
	$subManager.SetSubscription($currentSubscription)
	$resourceGroupManager = [ResourceGroupManager]::new()

	# Get a list of all the resource groups in buckets.
	$groupBuckets = $resourceGroupManager.GetGroupBuckets()
	
	# Only unlocked groups AND only ones that have no tags. 
	$uncompliantGroups = New-Object System.Collections.ArrayList
	
	foreach($unlockedGroup in $groupBuckets.Unlocked)
	{
		Write-Host("Unlocked Group: " + $unlockedGroup)
		$ugroup = $resourceGroupManager.GetGroup($unlockedGroup)
		if($ugroup)
		{
			$missing = $ugroup.FindMissingTags($expectedTags)

			if($missing.Count -gt 0)
			{
				# This is where a delete would occur.
				#$ugroup.Delete()
				$uncompliantGroups.Add($unlockedGroup) > $null
			}
		}
	}
	
	Write-Host("Unlocked Groups:")
	Write-Host(($groupBuckets.Unlocked | ConvertTo-Json))
	Write-Host("NonCompliant Groups:")
	Write-Host(($uncompliantGroups | ConvertTo-Json))
}