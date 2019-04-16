
class AzureResources {


	###############################################################
	# FindDeployments
	#
	#	Find the deploymnets of a specific resource type in an AzureRmContext
	#	subscription.
	#
	#	Params:
	#		resourceType : String resource type to find, i.e. 
	#				-resourceType 'Microsoft.MachineLearningServices/workspaces'
	#
	#	Returns:
	#		HashTable<[string]resource group, HashTable2>
	#			HashTable2<[string]resourceName, [string]resourceType
	###############################################################
	static [System.Collections.Hashtable] FindDeployments([string]$resourceType) {
		$returnList = @{}
		
		$resources = Get-AzureRmResource -ResourceType $resourceType
		foreach($res in $resources)
		{
			if($returnList.ContainsKey($res.ResourceGroupName) -eq $false)
			{
				$returnList.Add($res.ResourceGroupName,@{})
			}
			
			$returnList[$res.ResourceGroupName].Add($res.Name, $resourceType)
		}		
		return $returnList
	}
	
	###############################################################
	# GetResources
	#
	#	Get a list of all resources and the count of each resource
	#	type in a subscription. 
	#
	#
	#	Returns:
	#		HashTable<[string]resourceType, [int]resourceCount>
	###############################################################
	static [System.Collections.Hashtable]  GetAllResources() {
	
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
		
		return $returnTable
	}	

}