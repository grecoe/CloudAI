
# UPdate the CLI:
# https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest

# Ties together DataBricks managed resource groups to the owner group and adds a tag to the owner group
# to visually see the connection.

#Assumes an az login already happened.

$subId = 'YOUR_SUB_ID'
$dataBricksResourceGroups = New-Object System.Collections.ArrayList

$dataBricksResourceGroups.Add('databricks-rg-us2-jehrling-x24ialn7epsv2') > $null

az account set -s $subId


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

