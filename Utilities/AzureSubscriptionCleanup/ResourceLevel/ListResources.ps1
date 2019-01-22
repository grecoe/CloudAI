#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
# whatif - Run it as if we would delete but only display
#		   what would happen if executed.
#####################################################
param(
	[string]$subId,
	[switch]$login=$false,
	[switch]$help=$false
)

#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host ""
	Write-Host "This script will collect all resources in the provided Azure subscription and list"
	Write-Host "them in a file separated by region into a file called"
	Write-Host "[SUBSCRIPITON_ID]_resources.json"
	Write-Host ""
	Write-Host "You will be prompted to log in but MUST provide a subscription ID."
	Write-Host ""
	Write-Host "Parameters:"
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-login : Tells script to log into azure subscription, otherwise assumes logged in already"
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

Write-Host "Setting subscription ID : $subId"
Set-AzureRmContext -SubscriptionID $subId



#####################################################
# Collect all of the resource groups
#####################################################
Write-Host "Scanning subscription for resources, this could take a while....."


$resourceList = @{}

$resources = Get-AzureRmResource
foreach($res in $resources)
{
	if($resourceList.ContainsKey($res.Location) -eq $false)
	{
		$resourceList.Add($res.Location,@{})
	}
		
	if($resourceList[$res.Location].ContainsKey($res.ResourceType) -eq $false)
	{
		$resourceList[$res.Location].Add($res.ResourceType,0)
	}
	$resourceList[$res.Location][$res.ResourceType]++
}
	

#Create a directory
md -ErrorAction Ignore -Name $subId

$outputOverview = $resourceList | ConvertTo-Json -depth 100
$fileName = "list_resources.json"
Out-File -FilePath .\$subId\$fileName -InputObject $outputOverview

Write-Host("Completed")






