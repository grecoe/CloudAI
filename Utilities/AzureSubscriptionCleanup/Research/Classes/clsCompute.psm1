using module .\clsResources.psm1
using module .\clsResourceGroupManager.psm1

#############################################################################
#	Represents a single virtual machine (not in AMLS Cluster)
#############################################################################
class VirtualMachine {
	[string]$ResourceGroup 
	[string]$MachineName 
	[bool]$Running 
	[bool]$Deallocated  
	[bool]$Stopped  
	[string]$Sku 

	
	[void] Stop ([bool]$deallocate){
		if($this.Running)
		{
			if($deallocate)
			{
				$result = Stop-AzureRmVM -ResourceGroupName $this.ResourceGroup -Name $this.MachineName -Force
			}
			else
			{
				$result = Stop-AzureRmVM -ResourceGroupName $this.ResourceGroup -Name $this.MachineName -Force -StayProvisioned
			}
		}
	}
	
	[void] Start (){
		if($this.Stopped)
		{
			$result = Start-AzureRmVM -ResourceGroupName $this.ResourceGroup -Name $this.MachineName
		}
	}
}

#############################################################################
#	Represents an Azure Machine Learning Service Workspace
#############################################################################
class AMLSWorkspace {
	[string]$ResourceGroup 
	[string]$Workspace
	[string]$Details
	[System.Collections.ArrayList]$Clusters
	
	AMLSWorkspace(){
		$this.Clusters = New-Object System.Collections.ArrayList
	}
}

#############################################################################
#	Represents an cluster in a Azure Machine Learning Service Workspace
#############################################################################
class AMLSCluster {
	[string]$ComputeName 
	[string]$ComputeLocation
	[string]$ComputeType
	[string]$State
	[string]$Priority
	[string]$SKU
	[int]$CurrentNodes
	[int]$MaxNodes
	[int]$MinNodes
	[ResourceGroup]$ClusterGroup
}

class ComputeSummary {
	[int]$RunningTotal
	[int]$StoppedTotal
	[int]$DeallocatedTotal
	[System.Collections.HashTable]$SkuBreakdown
	
	ComputeSummary(){
		$this.SkuBreakdown = New-Object System.Collections.HashTable
	}
}

#############################################################################
#	Utility to collect compute resources in the subscription.
#############################################################################
class AzureCompute {
	[System.Collections.HashTable]$VirtualMachines=$null
	[System.Collections.ArrayList]$AMLSCompute=$null
	
	AzureCompute(){
		$this.ClearCache()
	}
	
	###############################################################
	# Clear the internals if you switch subscriptions so that the
	# correct information will be returned.
	###############################################################
	[void] ClearCache() {
		$this.VirtualMachines = New-Object System.Collections.HashTable
		$this.AMLSCompute = $null
	}

	###############################################################
	# Get an array of AMLSWorkspace instances for all AMLS details
	# across the subscription. If it's already been collected, just
	# return the cached information.
	#
	#
	###############################################################
	[System.Collections.ArrayList] GetAMLSComputeVms([ResourceGroupManager]$groupManager){
		Write-Host("Searching for AMLS Compute Details")
		$returnList = New-Object System.Collections.ArrayList
		
		if($this.AMLSCompute)
		{
			Write-Host("***Returning cached information***")
			$returnList = $this.AMLSCompute.Clone()
		}
		else
		{
			$resourceType = 'Microsoft.MachineLearningServices/workspaces'
			$workspaceDeployments = [AzureResources]::FindDeployments($resourceType)
	
			foreach($resourceGroup in $workspaceDeployments.Keys)
			{
				Write-Host("Group: " + $resourceGroup)
				foreach($workspace in $workspaceDeployments[$resourceGroup].Keys)
				{
					Write-Host("    Workspace: " + $workspace)
					# Now find out what we want 
					$computeListText = az ml computetarget list -g $resourceGroup -w $workspace
					$computeList = $computeListText | ConvertFrom-Json
			
					$amlsWorkspace = [AMLSWorkspace]::new()
					$amlsWorkspace.ResourceGroup = $resourceGroup
					$amlsWorkspace.Workspace = $workspace
					#$amlsWorkspace.Details = $computeListText
					
					foreach($compute in $computeList)
					{
						$computeDetailsText = az ml computetarget show -n $compute.name -g $resourceGroup -w $workspace -v
						$computeDetails = $computeDetailsText | ConvertFrom-Json
						
						$amlsCluster = [AMLSCluster]::new()
						$amlsCluster.ComputeName = $compute.name
						$amlsCluster.ComputeLocation = $computeDetails.properties.computeLocation
						$amlsCluster.ComputeType = $computeDetails.properties.computeType
						$amlsCluster.State = $computeDetails.properties.provisioningState
						$amlsCluster.Priority = $computeDetails.properties.properties.vmPriority
						$amlsCluster.SKU = $computeDetails.properties.properties.vmSize
						$amlsCluster.CurrentNodes = $computeDetails.properties.status.currentNodeCount
						$amlsCluster.MaxNodes = $computeDetails.properties.properties.scaleSettings.maxNodeCount
						$amlsCluster.MinNodes = $computeDetails.properties.properties.scaleSettings.minNodeCount
						
						if($groupManager -and ($amlsCluster.ComputeType -eq 'AKS'))
						{
							$groupPattern = "*" + $amlsCluster.ComputeName + "*"
							$associatedClusterGroup = $groupManager.FindGroup($groupPattern)
							if($associatedClusterGroup.Count -eq 1)
							{
								$amlsCluster.ClusterGroup = $associatedClusterGroup[0]
							}
						}
						
						$amlsWorkspace.Clusters.Add($amlsCluster) > $null
					}
					
					$returnList.Add($amlsWorkspace) > $null
				}
			}
			
			$this.AMLSCompute = $returnList.Clone()
		}		
		
		return $returnList
	}
	
	#########################################################################
	#	Collects a summary of AMLS compute resources. 
	# 	
	#	Internally calls GetAMLSComputeVms to see of we can get a cached version
	#	for expediency. 
	#
	#	Returns a ComputeSummary instance
	#########################################################################
	[ComputeSummary] GetAMLSSummary([ResourceGroupManager]$groupManager){
	
		$returnSummary = [ComputeSummary]::new()
		
		# If already called, this is the summary, otherwise, collect
		$details = $this.GetAMLSComputeVms($groupManager)
		
		foreach($workspace in $details)
		{
			foreach($cluster in $workspace.Clusters)
			{
				$returnSummary.RunningTotal += $cluster.CurrentNodes
				$returnSummary.DeallocatedTotal += ($cluster.MaxNodes - $cluster.CurrentNodes)
				
				if($cluster.SKU)
				{
					if($returnSummary.SkuBreakdown.ContainsKey($cluster.SKU))
					{
						$returnSummary.SkuBreakdown[$cluster.SKU] += $cluster.MaxNodes
					}
					else
					{
						$returnSummary.SkuBreakdown.Add($cluster.SKU,$cluster.MaxNodes)
					}
				}
			}
		}
		
		return $returnSummary;
	}
	
	#########################################################################
	#	Collects a list of virtual machines from the subscription. If the
	# 	resource group is null, searches the whole sub. If skuFilter is not
	#	null, sku type is searched with input string using -like
	#
	#	If the full command to issue has been issued already, return cached
	# 	details. 
	#########################################################################
	[System.Collections.ArrayList] GetVirtualMachines([string]$resourceGroup,[string]$skuFilter)
	{
		Write-Host("Searching for Virtual Machines")
		$returnList = New-Object System.Collections.ArrayList
		
		$vms = $null
		$command=$null
		$filter=$null
		# If a sku filter provided, prepare for it.
		if($skuFilter)
		{
			Write-Host("Filter VM with : " + $skuFilter)
			$filter = " | Where-Object {`$_.HardwareProfile.VmSize -like '" + $skuFilter + "'}"
		}
			
		# If a resource group is provided, prepare for that as well.
		if($resourceGroup)
		{
			Write-Host("Get VM Instances in resource group: " + $resourceGroup)
			$command = 'Get-AzureRmVM -ResourceGroupName ' + $resourceGroup
		}
		else
		{
			Write-Host("Get VM Instances in Subscription ")
			$command = 'Get-AzureRmVM '
		}
	
		# Build up the full command and execute it.
		$fullCommand = $command + $filter
		
		#Check to see if we've fulfilled this already
		if($this.VirtualMachines.ContainsKey($fullCommand))
		{
			Write-Host("***Returning cached information***")
			$returnList = $this.VirtualMachines[$fullCommand].Clone()
		}
		else
		{
			Write-Host("Executing: " + $fullCommand)
			$vms = Invoke-Expression $fullCommand
			
			foreach($vminst in $vms)
			{
				$status = $this.GetVirtualMachineStatus($vminst.ResourceGroupName,$vminst.Name)
				
				if($status)
				{
					$status.Sku = $vminst.HardwareProfile.VmSize
					$returnList.Add($status) > $null
				}
			}
			
			$this.VirtualMachines.Add($fullCommand, $returnList.Clone())
		}		
		
		return $returnList
	}
	
	#########################################################################
	#	Collects a summary of Virtual Machine compute resources. 
	# 	
	#	Internally calls GetVirtualMachines to see of we can get a cached version
	#	for expediency. 
	#
	#	Returns a ComputeSummary instance
	#########################################################################
	[ComputeSummary] GetVirtualMachineSummary([string]$resourceGroup,[string]$skuFilter){
	
		$returnSummary = [ComputeSummary]::new()
		
		# If already called, this is the summary, otherwise, collect
		$details = $this.GetVirtualMachines($resourceGroup, $skuFilter)
		
		foreach($machine in $details)
		{
			if($machine.Running)
			{
				$returnSummary.RunningTotal += 1
			}
			elseif($machine.Stopped)
			{
				$returnSummary.StoppedTotal += 1
			}
			else
			{
				$returnSummary.DeallocatedTotal += 1
			}
			
			
			if($machine.Sku)
			{
				if($returnSummary.SkuBreakdown.ContainsKey($machine.Sku))
				{
					$returnSummary.SkuBreakdown[$machine.Sku] += 1
				}
				else
				{
					$returnSummary.SkuBreakdown.Add($machine.Sku,1)
				}
			}
		}
		
		return $returnSummary;
	}
	
	#########################################################################
	#	Gets the detials of a specific virtual machine. 
	#########################################################################
	hidden [VirtualMachine] GetVirtualMachineStatus([string]$resourceGroup,[string]$instanceName) {
		
		$running=$false
		$stopped=$false
		$deallocated=$false
		#$sku=$null
		
		# Using the call below gives you running state but not SKU
		$vmStatus = Get-AzureRmVM -ErrorAction Stop -Status -ResourceGroupName $resourceGroup -Name $instanceName
		if($vmStatus)
		{
			foreach($status in $vmStatus.Statuses)
			{
				if($status.code -eq "PowerState/running")
				{
					$running=$true
				}
	
				if($status.code -eq "PowerState/deallocated")
				{
					$deallocated=$true
				}
			}
			
			$stopped = ( ($running -eq $false) -and ($deallocated -eq $false))
			
			# Get SKU
			# Using the call below doesn't give you running state, but gives you SKU
			#$vmStatusSku = Get-AzureRmVM -ErrorAction Stop -ResourceGroupName $resourceGroup -Name $instanceName
			#$sku=$vmStatusSku.HardwareProfile.VmSize
		}
	
		$vmInformation = [VirtualMachine]::new()
		$vmInformation.ResourceGroup =$resourceGroup
		$vmInformation.MachineName=$instanceName
		$vmInformation.Stopped=$stopped
		$vmInformation.Deallocated=$deallocated
		$vmInformation.Running=$running
		#$vmInformation.Sku=$sku
		
		return $vmInformation
	}	

}