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
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-resourceType : Required string of resource type. Example: Microsoft.CognitiveServices/accounts" 
	Write-Host "	-help : Shows this help message"
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
Write-Host "Log into Azure...."
#Login-AzureRmAccount
Write-Host ("Setting subscription ID : " + $subId + " to search for resources of type " + $resourceType)
Set-AzureRmContext -SubscriptionID $subId


#####################################################
# Collect all of the resource groups
#####################################################
Write-Host "Scanning subscription for resource groups, this could take a while....."

$resourceGroups = Get-AzureRmResourceGroup 

$resourceList = @{}
$groupcount = 0
foreach($group in $resourceGroups)
{
	Write-Host($group.ResourceGroupName)
	
	$resources = Get-AzureRmResource -ResourceGroupName $group.ResourceGroupName -ResourceType $resourceType

	Write-Host($resources | ConvertTo-Json)

	foreach($res in $resources)
	{
		Write-Host($res | ConvertTo-Json)
		
		if($resourceList.ContainsKey($res.Location) -eq $false)
		{
			$resourceList.Add($res.Location, @{})
		}
		
		$resourceList[$res.Location].Add($res.Name, $res.ResourceId)
	}
}

# Output the data to a file.....
$overview = New-Object PSObject -Property @{ 
		Subscription = $subId; 
		ResourceType = $resourceType; 
		Instances = $resourceList; 
	}

$outputOverview = $overview | ConvertTo-Json -depth 100
$fileName = $subId + "_detail.json"
Out-File -FilePath .\$fileName -InputObject $outputOverview

Write-Host("Results can be found here: " + $fileName)