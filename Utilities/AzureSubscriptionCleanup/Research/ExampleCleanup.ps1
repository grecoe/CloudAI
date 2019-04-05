################################################################
#	There are times when a sweep to clean up resources groups, 
#	for whatever reason, is a good idea.
#	
#	Resource groups get stale, team members leave the team, 
#	or for any other reason. 
#	
#	This example script assumes that the team has the following 
#	policies
#	
#	Every resource group created MUST 
#		- Have a delete lock applied to it once it's created
#		- Apply the following tags:
#			alias - Users alias who "owns" the group
#			project - The specific project it was created for
#			expires - A time field when the group should be cleared
#			
#	Now, we know that sometimes when the script runs someone may
#	have created a group and tagged it, but has not yet applied
#	tags to it. So for our clean up we will look to clean up:
#	
#		- Resource groups that do not have a lock on them.
#		- Resource groups that do not have the applied tags.
#		
#	If a group meets these two criteria, it will be deleted.
#
#	Just to be sure, by default this script will NOT delete 
#	anything, change the state of $doDelete to actually clean.
################################################################

. './Azure/Subscription.ps1'
. './Azure/ResourceGroup.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

################################################################
# Provide your subscription to verify
################################################################
$subId = 'YOUR_SUBSCRIPTION_ID' 
$requiredTags = ("alias", "project", "expires")

################################################################
# This flag is a validation that you WANT to delete 
################################################################
$doDelete=$false

# Now verify for script runner so they KNOW what they are doing. 
if($doDelete -eq $true)
{
	Write-Host "This run will delete all unlocked and untagged resource groups in the subscription."
	$response = Read-Host 'Are you sure you want to actually delete these?  (y/n) '
	if($response -ne "y")
	{
		Write-Host "Cancelling operations"
		break
	}
}


################################################################
# Set context so we don't get hung up later
################################################################
Write-Host("Setting context to sub....")
SetContext -subId $subId


################################################################
# Load up the resource group buckets to easily filter out the 
# unlocked resource groups easily. 
################################################################
Write-Host("Load group buckets....")
$unlockedGroupKey = "Unlocked"
$resourceGroups = GetResourceGroupBuckets -subId $subId
# Show the list
Write-Host(($resourceGroups | ConvertTo-Json))

################################################################
# Iterate over the unlocked groups and check the tags state. 
################################################################
$targetGroups = New-Object System.Collections.ArrayList
if($resourceGroups.ContainsKey($unlockedGroupKey))
{
	Write-Host("Iterate over unlocked groups")
	foreach($unlockedGroup in $resourceGroups[$unlockedGroupKey])
	{
		Write-Host("Load group details: " + $unlockedGroup)
		$groupDetails = LoadDetailedResourceGroup -resourceGroup $unlockedGroup
		
		$groupTags = ParseTags -tags $groupDetails.Tags
		$missingTags = FindMissingTags -tags $groupTags -expected $requiredTags
		
		# If any of the tags are missing, it's a target.
		if($missingTags.Count -gt 0)
		{
			$targetGroups.Add($unlockedGroup) > $null
			if($doDelete)
			{
				Write-Host("	Delete on missing tags: " +  ($missingTags -join ','))
				# For extra safety you could comment this out and nothing will happen.
				Remove-AzureRmResourceGroup -Force -Name $unlockedGroup
			}
		}
	}
}

# Post a final update
$comment = "The following groups would be deleted :"
if($doDelete)
{
	$comment = "The following groups were deleted :"
}

Write-Host("Of the unlocked groups: " )
Write-Host( ($resourceGroups[$unlockedGroupKey] | ConvertTo-Json))
Write-Host($comment)
Write-Host( ($targetGroups | ConvertTo-Json))
