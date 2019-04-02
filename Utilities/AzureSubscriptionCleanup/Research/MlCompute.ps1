# https://docs.microsoft.com/en-us/azure/machine-learning/service/reference-azure-machine-learning-cli

. './ResourceUtils.ps1'

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
#		sub : Subscription ID
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
	Param ([string]$sub)

	$resourceListDictionary = New-Object System.Collections.ArrayList

	Write-Host("MLCompute Set RM Context")
	$context = Set-AzureRmContext -SubscriptionID $sub

	Write-Host("Find MLCompute Deployments")
	$resourceType = 'Microsoft.MachineLearningServices/workspaces'
	$deployments = FindDeployments -sub $subId -resourceType $resourceType
		
	Write-Host("az login account set")
	$context = az account set -s $sub

	foreach($rgroup in $deployments.Keys)
	{
		foreach($wspace in $deployments[$rgroup].Keys)
		{
			# Now find out what we want 
			$computeListText = az ml computetarget list -g $rgroup -w $wspace
			$computeList = $computeListText | ConvertFrom-Json

			foreach($compute in $computeList)
			{
				Write-Host($compute.name)
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