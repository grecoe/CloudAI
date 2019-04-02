
###############################################################
# GetResources
#
#	Get a list of all resources and the count of each resource
#	type in a subscription. 
#
#	Params:
#		sub : Subscription to work within
#
#	Returns:
#		HashTable<[string]resourceType, [int]resourceCount>
###############################################################
function GetResources {
	Param ([string]$subId)

	$context = Set-AzureRmContext -SubscriptionID $subId

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
#		sub : Subscription to work within
#		resourceType : String resource type to find, i.e. 
#				-resourceType 'Microsoft.MachineLearningServices/workspaces'
#
#	Returns:
#		HashTable<[string]resource group, HashTable2>
#			HashTable2<[string]resourceName, [string]resourceType
###############################################################
function FindDeployments {
	Param ([string]$sub,[string]$resourceType)

	$context = Set-AzureRmContext -SubscriptionID $sub

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

