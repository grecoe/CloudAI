Using module .\clsSubscription.psm1
Using module .\clsResourceGroupManager.psm1

# Perform a login prior to calling this, first call collects the subscriptions.
$subManager = [SubscriptionManager]::new()
$currentSubscription = $null

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

	# Find a specific group and unlock it (or delete it)
	$testGroup = $resourceGroupManager.GetGroup('unknowngroup')
	if($testGroup)
	{
		# unlock it, find missing tags, or modify the tags in Azure 
		$testGroup.Unlock()
	}
	
	# Get a list of all the resource groups in buckets.
	$groupBuckets = $resourceGroupManager.GetGroupBuckets()
	Write-Host("Group Buckets")
	Write-Host(($groupBuckets | ConvertTo-Json))

	$groupSummary = $resourceGroupManager.GetGroupSummary()
	Write-Host("Group Summary")
	Write-Host(($groupSummary | ConvertTo-Json))
}