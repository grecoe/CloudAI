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
#			# Get deeper VM info for rg or whole sub
#			GetVirtualMachines [-subId] [-resourceGroup]
#			# Get VM summary for rg or whole sub
#			GetVirtualMachinesSummary [-subId] [-resourceGroup]
#			# Get VM status for specific instance
#			GetVmInformation [-subId] -resourceGroup -instanceName
#			# Stop a virtual machine, optionally deallocate
#			StopVirtualMachine [-subId] -resourceGroup -instanceName [flag]-deallocate
#			# Start a virtual machine
#			StartVirtualMachine [-subId] -resourceGroup -instanceName
#			# Summarize AML Compute Cluster Information
#			SummarizeComputeClusters -mlComputeInformation
#			# Find all AML Compute Clusters in subscription
#			FindMlComputeClusters [-subId]
#		
###############################################################

. './Azure/Resources.ps1'

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
	$vminstances = GetVirtualMachinesSummary -resourceGroup $null

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
#	$vminstances = GetVirtualMachinesSummary -subId $subId -resourceGroup $null
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
#		vmOverview : Output of GetVirtualMachinesSummary
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
#		[resourceGroup] : Resource Group to search, if null then search entire subscription
#		[skuFilter] : Optional filter for getting certain types. For example, 
#					  to get GPU machines, use '*NC*' as the filter. 
#
#	Returns:
#		List<PSObject>:			
#			Name : string
#			Instance : string
#			Running : bool
#			Deallocated : bool 
#			Stopped : bool 
#			Sku : VM Sku
###############################################################
function GetVirtualMachines {
	Param([string]$resourceGroup,[string]$subId,[string]$skuFilter)
	
	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	
	$machineList = New-Object System.Collections.ArrayList

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
	Write-Host("Executing: " + $fullCommand)
	$vms = Invoke-Expression $fullCommand
	
	foreach($vminst in $vms)
	{
		$status = GetVmInformation -includeSku -resourceGroup $vminst.ResourceGroupName -instanceName $vminst.Name
		if($status)
		{
			$machineList.Add($status) > $null
		}
	}
	
	$machineList
}

###############################################################
# GetVirtualMachinesSummary
#
#	Retrieves a collection of Virtual Machine stats for a specific
#	resource group in a subscription. 
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		[resourceGroup] : Resource Group to search, if null then search entire subscription
#
#	Returns:
#		PSObject:			
#			Total  - int 
#			Running - int
#			Stopped - int
#			Deallocated - int 
#			SKU - Hash Table , KEY = SKU[String], Value = Count[int]
###############################################################
function GetVirtualMachinesSummary {
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
		
		$status = GetVmInformation -resourceGroup $vminst.ResourceGroupName -instanceName $vminst.Name
		
		# Record the state count to the total.
		if($status.Running)
		{
			$runningVirtualMachines++
		}
		if($status.Deallocated)
		{
			$deallocatedVirtualMachines++
		}
		if($status.Stopped)
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
	
	$vmInformation = New-Object PSObject -Property @{ 
			Total =$totalVirtualMachines; 
			Running=$runningVirtualMachines;
			Stopped=$stoppedVirtualMachines;
			Deallocated=$deallocatedVirtualMachines;
			SKU=$virtualMachineSkus}
	
	$vmInformation
}

###############################################################
# GetVmInformation
#
#	Get the status of a given instance of a VM
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		resourceGroup : RG Name
#		instanceName : VM Name
#
#	Returns:
#		Object:
#			Name : string
#			Instance : string
#			Running : bool
#			Deallocated : bool 
#			Stopped : bool 
#			Sku : String, Only included if includeSku flag present
###############################################################
function GetVmInformation {
	Param([string]$subId, [string]$resourceGroup,[string]$instanceName,[switch]$includeSku)
	
	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	
	$running=$false
	$stopped=$false
	$deallocated=$false
	$sku=$null
	
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
		
		# Do we need SKU?
		if($includeSku -eq $true)
		{
			# Using the call below doesn't give you running state, but gives you SKU
			$vmStatusSku = Get-AzureRmVM -ErrorAction Stop -ResourceGroupName $resourceGroup -Name $instanceName
			$sku=$vmStatusSku.HardwareProfile.VmSize
		}
	}

	$vmInformation = New-Object PSObject -Property @{ 
			Name =$resourceGroup; 
			Instance=$instanceName;
			Stopped=$stopped;
			Deallocated=$deallocated;
			Running=$running;
			Sku=$sku}
	
	$vmInformation
}

###############################################################
# StopVirtualMachine
#
#	Stops a running virtual machine
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		resourceGroup : RG Name
#		instanceName : VM Name
#		deallocate : Switch, deallocates instead of stops
#
#	Returns: None
###############################################################
function StopVirtualMachine{
	Params([string]$subId, [string]$resourceGroup,[string]$instanceName,[switch]$deallocate)
	
	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	
	$status = GetVmInformation -resourceGroup $resourceGroup -instanceName $instanceName
	if($status)
	{
		if($status.Running)
		{
			if($deallocate)
			{
				$result = Stop-AzureRmVM -ResourceGroupName $resourceGroup -Name $instanceName -Force
			}
			else
			{
				$result = Stop-AzureRmVM -ResourceGroupName $resourceGroup -Name $instanceName -Force -StayProvisioned
			}
		}
	}
}

###############################################################
# StartVirtualMachine
#
#	Starts a stopped virtual machine
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		resourceGroup : RG Name
#		instanceName : VM Name
#
#	Returns: None
###############################################################
function StartVirtualMachine{
	Params([string]$subId, [string]$resourceGroup,[string]$instanceName)
	
	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	
	$status = GetVmInformation -resourceGroup $resourceGroup -instanceName $instanceName
	if($status)
	{
		if($status.Stopped)
		{
			$result = Start-AzureRmVM -ResourceGroupName $resourceGroup -Name $instanceName
		}
	}
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
			Write-Host("    Workspace: " + $wspace)
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