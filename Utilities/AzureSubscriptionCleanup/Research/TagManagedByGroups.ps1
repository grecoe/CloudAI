################################################################
#	Some resource groups in Azure are managed by other resource 
#	groups. In particular, DataBricks clusters are created from 
#	DataBricks workspaces, but in a separate resource group than
#	the workspace.
#	
#	These groups have a ReadOnly system lock applied to them and 
#	hence cannot be tagged for auditing purposes. 
#	
#	Certainly you can search for the managedBy groups in many ways
#	but this code finds the managing resource group and applies a
#	manages tag to that making it much easier to find and also 
#	visually available to portal users. 
################################################################


# You may need to update your Azure CLI to do this. 
# https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest

. './Azure/Subscription.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

# Provide your subscription to verify
$subId = 'YOUR_SUBSCRIPTION_ID' 

# Set context so we don't get hung up later
SetContext -subId $subId


# Because we are assuming we know what we want to tie to, create a list of any 
# resource group that starts with 'databricks-' as these are the databricks
# cluster resource groups.
$dataBricksResourceGroups = New-Object System.Collections.ArrayList
$dataBricksResourceGroups.Add('databricks-rg-us2-jehrling-x24ialn7epsv2') > $null


foreach($group in $dataBricksResourceGroups)
{
	Write-Host("Working on group " + $group);
	$infoObj = (az group show -g $group) | ConvertFrom-Json
	
	$managedByGroup=$null
	if($infoObj.managedBy)
	{
		$resourceObject = (az resource show --ids $infoObj.managedBy) | ConvertFrom-Json
		$managedByGroup = $resourceObject.resourceGroup
		
		Write-Host("Group " + $group + " is managed by " + $managedByGroup)
	}

	if($managedByGroup)
	{
		
		$managedByObj = (az group show -g $managedByGroup) | ConvertFrom-Json
		$tagsList = New-Object System.Collections.ArrayList
		
		if($managedByObj.Tags)
		{
			Write-Host("Obtaining existing tags to preserve them")
			$managedByObj.Tags.PSObject.Properties | Foreach { $tagsList.Add($_.Name +"=" + "'" + $_.Value +"'") > $null }
		}
	
		Write-Host("Add in new tags")
		$tagsList.Add("manages='" + $group + "'") > $null

		$tagInput = $null
		foreach($tag in $tagsList)
		{	
			$tagInput += " " + $tag
		}

		# Create the command string and execute it.
		Write-Host("Updating " + $managedByGroup + " with new tag list " + $tagInput)
		$commandString = "az group update -n " + $managedByGroup + " --tags " + $tagInput
		Invoke-Expression $commandString
	}
}

