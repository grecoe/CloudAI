# https://docs.microsoft.com/en-us/azure/machine-learning/service/reference-azure-machine-learning-cli

. './ResourceUtils.ps1'

# Return has RG, WS, ComputeName, ComputeDetails

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
#			Details: Object (unknown content at this time, broken API)
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
				$computeDetailsText = az ml computetarget show -n $compute.name -g $rgroup -w $wspace
				$computeDetails = $computeDetailsText | ConvertFrom-Json
				
				$computeResult = New-Object PSObject -Property @{ 
					ResourceGroup =$rgroup; 
					Workspace=$wspace;
					Compute=$compute.name;
					Details=$computeDetails}
				$resourceListDictionary.Add($computeResult) > $null
			}
		}
	}
	
	$resourceListDictionary
}