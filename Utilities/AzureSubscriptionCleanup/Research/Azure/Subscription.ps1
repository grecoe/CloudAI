

###############################################################
# SetContext
#
#	Global way to set context for az and AzureRM commands to 
#	take the guess work out of when to swtich context for 
#	batched commands.
#
#	Params:
#		subId : Subscription to set 
#
#	Returns: None
###############################################################
function SetContext {
	Param([string]$subId)

	$context = Set-AzureRmContext -SubscriptionID $subId
	$context = az account set -s $subId
}

###############################################################
# GetSubscriptionOverview
#
#	Collect information about resources in a subscription
#
#	Params:
#		subId : Subscription to scan
#
#	Returns: PSObject
#		GroupInfo - Results of GetResourceGroupSummary
#		Compute - Results of GetVirtualMachineComputeSummary
#		Resources = Results of GetResources
###############################################################
function GetSubscriptionOverview {
	Param([string]$subId)

	SetContext -subId $subId
	
	$computeSummary = GetVirtualMachineComputeSummary
	$groups = GetResourceGroupInfo 
	$groupSummary = GetResourceGroupSummary -groups $groups
	$resources = GetResources 

	$subSummary = New-Object PSObject -Property @{ 
		GroupInfo=$groupSummary;
		Compute=$computeSummary;
		Resources=$resources}

	$subSummary
}