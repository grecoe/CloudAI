#############################################################################
#	In this example compute, we collect information on Virutal Machines and
# 	Azure Machine Learning Service compute.
#############################################################################

. './clsSubscription.ps1'
. './clsResources.ps1'
. './clsCompute.ps1'
. './clsResourceGroupManager.ps1'

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
	$resourceGroupManager = [ResourceGroupManager]::new()

	####################################################################
	# Collect Compute Information
	####################################################################
	$amlsSummary = $azureCompute.GetAMLSSummary()
	$vmSummary = $azureCompute.GetVirtualMachineSummary($null,$null)

	####################################################################
	# Collect Resource Information
	####################################################################
	$resourceList = [AzureResources]::GetAllResources()
	
	####################################################################
	# Collect Resource Group Information
	####################################################################
	$resourceGroupBuckets = $resourceGroupManager.GetGroupBuckets()
	$resourceGroupSummary = $resourceGroupManager.GetGroupSummary()

	Write-Host("***Resource Group Summary***")
	Write-Host(($resourceGroupSummary | ConvertTo-Json))	
	Write-Host("***Resource Group Buckets***")
	Write-Host(($resourceGroupBuckets | ConvertTo-Json))
	Write-Host("***AMLS Compute***")
	Write-Host(($amlsSummary | ConvertTo-Json))
	Write-Host("***Virtual Machines***")
	Write-Host(($vmSummary | ConvertTo-Json))
	Write-Host("***Resource Lists***")
	Write-Host(($resourceList | ConvertTo-Json))

}