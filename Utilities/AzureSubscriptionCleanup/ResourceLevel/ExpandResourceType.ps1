#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
# whatif - Run it as if we would delete but only display
#		   what would happen if executed.
#####################################################
param(
	[string]$subId,
	[string]$resourceType,
	[string]$resourceGroup,
	[switch]$login=$false,
	[switch]$deleteResources=$false,
	[switch]$help=$false
)

#https://docs.microsoft.com/en-us/powershell/module/azurerm.resources/remove-azurermresource?view=azurermps-6.13.0

#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host ""
	Write-Host "This script will collect a list of all resources by a given resource type in the"
	Write-Host "provided Azure Subscription. Output will be written to a file called"
	Write-Host "[SUBSCRIPITON_ID]_details.json"
	Write-Host ""
	Write-Host "You will be prompted to log in but MUST provide a subscription ID."
	Write-Host ""
	Write-Host "Parameters:"
	Write-Host "	-subId [subid]: Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-resourceType [restype]: Required string of resource type. Example: Microsoft.CognitiveServices/accounts" 
	Write-Host "	-resourceGroup [groupname]: Optional string identifying a specific resource group." 
	Write-Host "	-deleteResources : Optional, presence means to delete resources found. " 
	Write-Host "	-login : Tells script to log into azure subscription, otherwise assumes logged in already"
	Write-Host "	-help : Optional, presence means to show this help message"
	break
}

#####################################################
# Verify that subId is actually provided
#####################################################
if(-not $subId)
{
	Write-Host "-subId is a required parameter. Run the script with -help to get more information."
	break
}

if(-not $resourceType)
{
	Write-Host "-resourceType is a required parameter. Example: Microsoft.CognitiveServices/accounts"
	break
}


#####################################################
# Log in and set to the sub you want to see
#####################################################
if($login -eq $true)
{
	Write-Host "Log into Azure...."
	Login-AzureRmAccount
}
else
{
	Write-Host "Bypassing Azure Login...."
}

Write-Host ("Setting subscription ID : " + $subId )
Set-AzureRmContext -SubscriptionID $subId

Write-Host ("")
if(-not $resourceGroup)
{
	Write-Host ("Searching all resource groups for resources of type : " + $resourceType)
}
else
{
	Write-Host ("Searching resource group " + $resourceGroup + " for resources of type : "  + $resourceType)
}


#####################################################
# Collect all of the resource groups
#####################################################


$resourceList = @{}
$groupcount = 0
if(-not $resourceGroup)
{
	#####################################################
	# A resource group was not provided so scan over all
	# of the resource groups in the subscription.
	#####################################################
	Write-Host("Scanning subscription for all resource groups, this could take a while.....")

	$resourceGroups = Get-AzureRmResourceGroup 

	foreach($group in $resourceGroups)
	{
		Write-Host("Scanning resource group: " + $group.ResourceGroupName)
	
		$resources = Get-AzureRmResource -ResourceGroupName $group.ResourceGroupName -ResourceType $resourceType

		foreach($res in $resources)
		{
			Write-Host("Found resource: " + $res.Name)

			if($resourceList.ContainsKey($res.Location) -eq $false)
			{
				$resourceList.Add($res.Location, @{})
			}
		
			$resourceList[$res.Location].Add($res.Name, $res.ResourceId)
		}
	}
}
else
{
	#####################################################
	# A resource group was provided so scan only the 
	# provided resource group.
	#####################################################
	Write-Host("Scanning resource group : " + $resourceGroup)
	
	$resources = Get-AzureRmResource -ResourceGroupName $resourceGroup -ResourceType $resourceType

	foreach($res in $resources)
	{
		Write-Host("Found resource: " + $res.Name)
		
		if($resourceList.ContainsKey($res.Location) -eq $false)
		{
			$resourceList.Add($res.Location, @{})
		}
	
		$resourceList[$res.Location].Add($res.Name, $res.ResourceId)
	}
}
Write-Host ""


#####################################################
# Check for the deleteResources flag. If it's set give
# the user one last chance to back out of the deletion.
#####################################################
if($deleteResources -eq $true)
{
	Write-Host "The deleteResources flag was set. Running in this mode will delete all resources found."
	$response = Read-Host 'Are you sure you want to delete all resources found? (y/n) '
	if($response -ne "y")
	{
		Write-Host "Cancelling the delete operations on found resources."
		break
	}
	else
	{
		Write-Host "Deleting found resources."

		foreach($regionKey in $resourceList.Keys)
		{
			Write-Host("Found in region : " + $regionKey)
			foreach($resourceKey in $resourceList[$regionKey].Keys)
			{
				Write-Host("Deleting " + $resourceType + " named " + $resourceKey + " in region " + $regionKey)
				Remove-AzureRmResource -Force -ResourceId $resourceList[$regionKey].Item($resourceKey)
			}
		}
	}
}


# Output the data to a file.....
$overview = New-Object PSObject -Property @{ 
		Subscription = $subId; 
		ResourceType = $resourceType; 
		Instances = $resourceList; 
	}

#Create a directory
md -ErrorAction Ignore -Name $subId
	
$outputOverview = $overview | ConvertTo-Json -depth 100
$fileName = "resource_detail.json"
Out-File -FilePath .\$subId\$fileName -InputObject $outputOverview

Write-Host("Completed")