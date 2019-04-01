
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
	Param ([string]$sub)

	$context = Set-AzureRmContext -SubscriptionID $sub

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

###############################################################
# GetVirtualMachines
#
#	Retrieves a collection of Virtual Machine stats for a specific
#	resource group in a subscription. 
#
#	Params:
#		sub : Subscription to work within
#		resourceGroup : Resource Grup to search.
#
#	Returns:
#		PSObject:			
#		Total  - int 
#		Running - int
#		Stopped - int
#		Deallocated - int 
#		SKU - Hash Table , KEY = SKU[String], Value = Count[int]
###############################################################
function GetVirtualMachines {
	Param([string]$resourceGroup,[string]$sub)
	
	$context = Set-AzureRmContext -SubscriptionID $sub
	
	$totalVirtualMachines = 0
	$runningVirtualMachines=0
	$deallocatedVirtualMachines=0
	$stoppedVirtualMachines=0
	$virtualMachineSkus=@{}
	
	$vms = Get-AzureRmVM -ResourceGroupName $resourceGroup
	foreach($vminst in $vms)
	{
		$totalVirtualMachines++
		$vmStatus = Get-AzureRmVM -ErrorAction Stop -Status -ResourceGroupName $resourceGroup -Name $vminst.Name
		if($vmStatus)
		{
			# Get the state of the VM
			$running=$false
			$deallocated=$false
			foreach($status in $vmStatus.Statuses)
			{
				if($status.code -eq "PowerState/running")
				{
					$running=$true
					break
				}
				if($status.code -eq "PowerState/deallocated")
				{
					$deallocated=$true
					break
				}
			}
			
			# Record the state count to the total.
			if($running)
			{
				$runningVirtualMachines++
			}
			elseif($deallocated)
			{
				$deallocatedVirtualMachines++
			}
			else
			{
				$stoppedVirtualMachines++
			}

			# Capture the size
			if($virtualMachineSkus.ContainsKey($vminst.HardwareProfile.VmSize) -eq $false)
			{
				$virtualMachineSkus.Add($vminst.HardwareProfile.VmSize,1)
			}
			else
			{
				$virtualMachineSkus[$vminst.HardwareProfile.VmSize]++
			}
		}
	}
	
	$vmInformation = New-Object PSObject -Property @{ 
			Total =$totalVirtualMachines; 
			Running=$runningVirtualMachines;
			Stopped=$stoppedVirtualMachines;
			Deallocated=$deallocatedVirtualMachines;
			SKU=$virtualMachineSkus}
	
	$vmInformation
}