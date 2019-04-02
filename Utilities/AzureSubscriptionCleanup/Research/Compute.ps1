###############################################################
# Compute.ps1
#
#	Contains scripts to collect Virtual Machine and AML Worspace
#	cluster information. 
#
#	These two types of resources tend to be the biggest spend items
#	for most teams.
#
#	The AML Compute relies on an extension for az ml in Powershell
# 	https://docs.microsoft.com/en-us/azure/machine-learning/service/reference-azure-machine-learning-cli
#
#	FUNCTIONS AVAILABLE
#			# Summary of all VM's in subscription
#			GetVirtualMachineComputeSummary [-subId]
#			# Merge VM list and AML Cluster list of machines
#			MergeComputeResources -mlClusterOverview -vmOverview
#			# Get VM info for rg or whole sub
#			GetVirtualMachines [-subId] [-resourceGroup]
#			# Summarize AML Compute Cluster Information
#			SummarizeComputeClusters -mlComputeInformation
#			# Find all AML Compute Clusters in subscription
#			FindMlComputeClusters [-subId]
#		
###############################################################

. './Resources.ps1'

###############################################################
# GetVirtualMachineComputeSummary
#
#	Collects and aggregates the information related to VM instances
#	in a subscription.
#
#	This includes standard Virtual Machines as well as machines
#	associated with an AML cluster.
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#
#	Returns:
#		PSObject:			
#			Total  - int 
#			Running - int
#			Stopped - int
#			Deallocated - int 
#			SKU - Hash Table , KEY = SKU[String], Value = Count[int]
###############################################################
function GetVirtualMachineComputeSummary {
	Param($subId)

	# Inner functions here will set context
	$mlCompute = FindMlComputeClusters -subId $subId
	$mlsummary = SummarizeComputeClusters -mlComputeInformation $mlCompute
	$vminstances = GetVirtualMachines -resourceGroup $null

	$mergedResults = MergeComputeResources -mlClusterOverview $mlsummary -vmOverview $vminstances
	
	#This is the merged results of VM and AMLCluster information
	$mergedResults
}


###############################################################
# MergeComputeResources
#
#	Determines total VM usage in a subscription by merging the VmSize
#	information with the ml compute cluster information.
#
#	#Supply your sub ID
#	$subId = 'YOUR_SUB_ID' 
#	
#	#Get Virtual Machines
#	$vminstances = GetVirtualMachines -subId $subId -resourceGroup $null
#	
#	#Get ML Compute Clusters
#	$mlCompute = FindMlComputeClusters -sub $subId
#	
#	#Summarize Compute Clusters
#	$mlsummary = SummarizeComputeClusters -mlComputeInformation $mlCompute
#	
#	#Merge the VM and the Compute Clusters to get a full picture
#	$mergedResults = MergeComputeResources -mlClusterOverview $mlsummary -vmOverview $vminstances
#	
#	Params:
#		mlClusterOverview : Output of SummarizeComputeClusters
#		vmOverview : Output of GetVirtualMachines
#
#	Returns: -vmOverview object param updated with content from the 
#			 -mlClusterOverview object.
###############################################################
function MergeComputeResources {
	Param($mlClusterOverview, $vmOverview)
	
	$returnValue = $null
	
	if($mlClusterOverview -and $vmOverview)
	{
		$returnValue = $vmOverview.PSObject.Copy()
		$returnValue.SKU = $vmOverview.SKU.Clone()
		
		foreach($clusterInfo in $mlClusterOverview)
		{
			$returnValue.Total += $clusterInfo.TotalMachines
			$returnValue.Running += $clusterInfo.ActiveMachines
			$returnValue.Deallocated += ($clusterInfo.TotalMachines - $clusterInfo.ActiveMachines)

			foreach($sku in $clusterInfo.SKU.Keys)
			{	
				if($returnValue.SKU.ContainsKey($sku))
				{
					$returnValue.SKU[$sku] += $clusterInfo.SKU[$sku]
				}
				else
				{
					$returnValue.SKU.Add($sku, $clusterInfo.SKU[$sku])
				}
			}
		}
	}

	$returnValue
}

###############################################################
# GetVirtualMachines
#
#	Retrieves a collection of Virtual Machine stats for a specific
#	resource group in a subscription. 
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		resourceGroup : Resource Group to search, if null then search entire subscription
#
#	Returns:
#		PSObject:			
#			Total  - int 
#			Running - int
#			Stopped - int
#			Deallocated - int 
#			SKU - Hash Table , KEY = SKU[String], Value = Count[int]
###############################################################
function GetVirtualMachines {
	Param([string]$resourceGroup,[string]$subId)
	
	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	
	$totalVirtualMachines = 0
	$runningVirtualMachines=0
	$deallocatedVirtualMachines=0
	$stoppedVirtualMachines=0
	$virtualMachineSkus=@{}
	
	$vms = $null
	if($resourceGroup)
	{
		Write-Host("Get VM Instances in resource group: " + $resourceGroup)
		$vms = Get-AzureRmVM -ResourceGroupName $resourceGroup
	}
	else
	{
		Write-Host("Get VM Instances in Subscription ")
		$vms = Get-AzureRmVM
	}

	foreach($vminst in $vms)
	{
		$totalVirtualMachines++
		$vmStatus = Get-AzureRmVM -ErrorAction Stop -Status -ResourceGroupName $vminst.ResourceGroupName -Name $vminst.Name
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

###############################################################
# SummarizeComputeClusters
#
#	Create a summary of the data from FindMlComputeClusters
#
#	Params:
#		mlComputeInformation : Output of FindMlComputeClusters
#
#	Returns:
#		Object:
#			TotalMachines : int
#			ActiveMachines : int
#			Compute : String
#			Details: Object 
#				ComputeLocation - String
#				ComputeType - string
#				SKU - Hashtable<[String]SKU name, [int]count>
###############################################################
function SummarizeComputeClusters {
	Param ($mlComputeInformation)
	
	$summaryInfo = @{}
	$totalAllocation=0
	$currentAllocation=0
	foreach($clusterInfo in $mlComputeInformation)
	{
		$totalAllocation += $clusterInfo.Details.MaxNodes
		$currentAllocation += $clusterInfo.Details.CurrentNodes
		
		# There is a chance there are no machines here, so make sure you have something first
		if($clusterInfo.Details.SKU)
		{
			if($summaryInfo.ContainsKey($clusterInfo.Details.SKU) -eq $false)
			{
				$summaryInfo.Add($clusterInfo.Details.SKU,0)
			}
			$summaryInfo[$clusterInfo.Details.SKU] += $clusterInfo.Details.MaxNodes
		}
	}
	
	$summary = New-Object PSObject -Property @{ 
				TotalMachines=$totalAllocation;
				ActiveMachines=$currentAllocation;
				SKU=$summaryInfo}
	
	$summary
}


###############################################################
# FindMlComputeClusters
#
#	Scan a subscription to find all ML workspaces and the assocated
#	compute resources contained therein. 
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#
#	Returns:
#		List<Object>:
#			ResourceGroup : String
#			Workspace : String
#			Compute : String
#			Details: Object 
#				ComputeLocation - String
#				ComputeType - string
#				State - String
#				Priority - String
#				SKU - String
#				CurrentNodes - int
#				MaxNodes - int
#				MinNodes - int
###############################################################
function FindMlComputeClusters {
	Param ([string]$subId)

	$resourceListDictionary = New-Object System.Collections.ArrayList

	Write-Host("Finding ML Compute Clusters")
	if($subId)
	{
		Write-Host("Switch context")
		$context = Set-AzureRmContext -SubscriptionID $subId
	}

	$resourceType = 'Microsoft.MachineLearningServices/workspaces'
	$deployments = FindDeployments -subId $subId -resourceType $resourceType
		
	if($subId)
	{
		$context = az account set -s $subId
	}
	
	Write-Host("Parse Deployments")
	foreach($rgroup in $deployments.Keys)
	{
		Write-Host("Group: " + $rgroup)
		foreach($wspace in $deployments[$rgroup].Keys)
		{
			Write-Host("    Workspace: " + $rgroup)
			# Now find out what we want 
			$computeListText = az ml computetarget list -g $rgroup -w $wspace
			$computeList = $computeListText | ConvertFrom-Json

			foreach($compute in $computeList)
			{
				$computeDetailsText = az ml computetarget show -n $compute.name -g $rgroup -w $wspace -v
				$computeDetails = $computeDetailsText | ConvertFrom-Json
				
				$trimmedComputeDetails = New-Object PSObject -Property @{ 
					ComputeLocation=$computeDetails.properties.computeLocation;
					ComputeType=$computeDetails.properties.computeType;
					State=$computeDetails.properties.provisioningState;
					Priority=$computeDetails.properties.properties.vmPriority;
					SKU=$computeDetails.properties.properties.vmSize;
					CurrentNodes=$computeDetails.properties.status.currentNodeCount;
					MaxNodes=$computeDetails.properties.properties.scaleSettings.maxNodeCount;
					MinNodes=$computeDetails.properties.properties.scaleSettings.minNodeCount}
					
	
				$computeResult = New-Object PSObject -Property @{ 
					ResourceGroup =$rgroup; 
					Workspace=$wspace;
					Compute=$compute.name;
					Details=$trimmedComputeDetails}
				$resourceListDictionary.Add($computeResult) > $null
			}
		}
	}
	
	$resourceListDictionary
}