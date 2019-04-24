
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
	#			HashTable2<[string]resourceName, HashTable3>
	#				HashTable3{Keys are SKU and Location
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
			
			$details = @{}
			if($res.Sku)
			{
				$details.Add("SKU" , $res.Sku.Name)
			}
			$details.Add("Location" , $res.Location)
			
			$returnList[$res.ResourceGroupName].Add($res.Name, $details)
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

	###############################################################
	# GetGroupResources
	#
	#	Get a list of resources in a specific group.
	#
	#
	#	Returns:
	#		HashTable<[string]resourceName, [string]resourceType>
	###############################################################
	static [System.Collections.Hashtable]  GetGroupResources([string]$resourceGroup) {
	
		$returnTable = @{}
		
		$allResources = Get-AzureRmResource -ResourceGroupName $resourceGroup
		foreach($res in $allResources)
		{
			$returnTable.Add($res.Name, $res.ResourceType)
		}
		
		return $returnTable
	}	

	static [void]  DeleteResource([string]$resourceName, [string]$resourceType) {
		Remove-AzureRmResource -ResourceName $resourceName -ResourceType $resourceType
	}	

	
}