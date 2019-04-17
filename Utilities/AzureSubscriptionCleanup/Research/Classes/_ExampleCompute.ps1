#############################################################################
#	In this example compute, we collect information on Virutal Machines and
# 	Azure Machine Learning Service compute.
#############################################################################

using module .\clsSubscription.psm1
using module .\clsResourceGroupManager.psm1 
using module .\clsCompute.psm1 

# Perform a login prior to calling this, first call collects the subscriptions.
$subManager = [SubscriptionManager]::new()
$currentSubscription = $null

# Filter on subscriptions by a name or partial name 
$subscriptionNameToFind="DevOps"
Write-Host("Searching for:  " + $subscriptionNameToFind )
$result = $subManager.FindSubscription($subscriptionNameToFind)

# Possible to get more than one result, so....be careful.
if($result.Count -eq 1)
{
	# Set this subscription as the current subscription to work on.
	$currentSubscription = $result[0]
	$subManager.SetSubscription($currentSubscription)

	
	Write-Host("Working with subscription " + $currentSubscription.Name)

	# Create instance of Azure Compute
	$azureCompute = [AzureCompute]::new()

	####################################################################
	# Collect AMLS Compute cluster information
	####################################################################
	$amlsComputeDetails = $azureCompute.GetAMLSComputeVms($null)
	# Call again and it returns cached information
	$amlsComputeDetails = $azureCompute.GetAMLSComputeVms($null)
	$amlsSummary = $azureCompute.GetAMLSSummary()

	####################################################################
	# Collect standard VM information
	####################################################################
	$virtualMachines = $azureCompute.GetVirtualMachines($null,'*nc*')
	# Call again and it returns cached information
	$virtualMachines = $azureCompute.GetVirtualMachines($null,'*nc*')
	$vmSummary = $azureCompute.GetVirtualMachineSummary($null,'*nc*')
	
	Write-Host("AMLS Summary:")
	Write-Host(($amlsSummary | ConvertTo-Json))
	Write-Host("VirtualMachine Summary:")
	Write-Host(($vmSummary | ConvertTo-Json))

	foreach($amlsDetails in $amlsComputeDetails)
	{
		Write-Host("AMLS Workspace Details")
		Write-Host(($amlsDetails|ConvertTo-Json))
	}

	Write-Host("Virtual Machines:")
	Write-Host(($virtualMachines | ConvertTo-Json))

}