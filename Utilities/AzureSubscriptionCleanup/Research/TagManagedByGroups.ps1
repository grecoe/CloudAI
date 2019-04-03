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
. './Azure/ResourceGroup.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

# Provide your subscription to verify
$subId = 'YOUR_SUBSCRIPTION_ID' 

# Set context so we don't get hung up later
SetContext -subId $subId

# Because we are assuming we know what we want to tie to, create a list of any 
# resource group that starts with 'databricks-' as these are the databricks
# cluster resource groups.
$dataBricksResourceGroups = New-Object System.Collections.ArrayList
$dataBricksResourceGroups.Add('databricks-rg-us2-XXXXXXX') > $null


foreach($group in $dataBricksResourceGroups)
{
	Write-Host("Working on group " + $group);

	$rgInfo = LoadDetailedResourceGroup -resourceGroup $group
	
	if($rgInfo.ManagedByResourceGroup)
	{
		$managedRgInfo = LoadDetailedResourceGroup -resourceGroup $rgInfo.ManagedByResourceGroup
		$tagsList = @{}
		
		if($managedRgInfo.Tags)
		{
			Write-Host("Obtaining existing tags to preserve them")
			$managedRgInfo.Tags.PSObject.Properties | Foreach { $tagsList.Add($_.Name, $_.Value)  }
		}
	
		Write-Host("Add in desired new tags....")
		if($tagsList.ContainsKey("manages"))
		{
			$tagsList["manages"] = $rgInfo.Name
		}
		else
		{
			$tagsList.Add("manages", $rgInfo.Name) 
		}

		Write-Host("Updating Managing Resource Group")
		ModifyGroupTags -groupName $managedRgInfo.Name -tags $tagsList
	}
}

