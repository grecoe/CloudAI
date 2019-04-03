###############################################################
# Resources.ps1
#
#	Contains scripts to work on individual resources
#
#
#	FUNCTIONS AVAILABLE
#			# List all resources in a subscription
#			GetResources -subId
#			# Find deployments of specific resource types
#			FindDeployments -subId -resourceType
###############################################################


###############################################################
# GetResources
#
#	Get a list of all resources and the count of each resource
#	type in a subscription. 
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#
#	Returns:
#		HashTable<[string]resourceType, [int]resourceCount>
###############################################################
function GetResources {
	Param ([string]$subId)

	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}

	$returnTable = @{}
	
	$allResources = Get-AzureRmResource
	foreach($res in $allResources)
	{
		if($returnTable.ContainsKey($res.ResourceType) -eq $false)
		{
			$returnTable.Add($res.ResourceType,1)
		}
		else
		{
			$returnTable[$res.ResourceType]++
		}
	}
	
	$returnTable
}

###############################################################
# FindDeployments
#
#	Find the deploymnets of a specific resource type in an AzureRmContext
#	subscription.
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		resourceType : String resource type to find, i.e. 
#				-resourceType 'Microsoft.MachineLearningServices/workspaces'
#
#	Returns:
#		HashTable<[string]resource group, HashTable2>
#			HashTable2<[string]resourceName, [string]resourceType
###############################################################
function FindDeployments {
	Param ([string]$subId,[string]$resourceType)

	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}

	# <rgname, hastable> , <hastable> = < name, type>
	$resourceListDictionary = @{}
	
	$resources = Get-AzureRmResource -ResourceType $resourceType
	foreach($res in $resources)
	{
		if($resourceListDictionary.ContainsKey($res.ResourceGroupName) -eq $false)
		{
			$resourceListDictionary.Add($res.ResourceGroupName,@{})
		}
		
		$resourceListDictionary[$res.ResourceGroupName].Add($res.Name, $resourceType)
	}
	
	$resourceListDictionary
}

