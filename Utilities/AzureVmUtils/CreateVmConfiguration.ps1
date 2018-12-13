#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
#####################################################
param(
	[string]$subId,
	[switch]$help=$false
)

#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host ""
	Write-Host "This script will collect a list of virtual machines per Azure Resource Group"
	Write-Host "from a provided Azure Subscription. Output will be written to a file called"
	Write-Host "SUBSCRIPITON_ID.json and can then be used, if renamed to VMConfiguration.json"
	Write-Host "with the AzureVmStateChange.ps1 script to bulk change VM states."
	Write-Host ""
	Write-Host "You will be prompted to log in but MUST provide a subscription ID."
	Write-Host ""
	Write-Host "Parameters:"
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
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
Write-Host "Log into Azure...."
Login-AzureRmAccount
Write-Host "Setting subscription ID : $subId"
Set-AzureRmContext -SubscriptionID $subId

#####################################################
# Collect all of the resource groups
#####################################################
Write-Host "Scanning subscription for resource groups, this could take a while....."

$resourceGroups = Get-AzureRmResourceGroup #| Where-Object {$_.ResourceGroupName -eq "dangtestdel"}

#####################################################
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################
$allVmCollections = New-Object System.Collections.ArrayList
foreach($group in $resourceGroups)
{
	$vms = Get-AzureRmVM -ResourceGroupName $group.ResourceGroupName
	
	$collectionGroup = @{}
	$vmcollection = New-Object System.Collections.ArrayList
	
	Write-Host("Collect VMS in resource group " + $group.ResourceGroupName)
	foreach($vminst in $vms)
	{
		$vmcollection.Add($vminst.Name) > $null
	}
	
	if($vmcollection.Count -gt 0)
	{
		$vmgroup = New-Object PSObject -Property @{ Name = $group.ResourceGroupName; VirtualMachines = $vmCollection }
		$allVmCollections.Add($vmgroup) > $null
	}
}

$rgCollection = New-Object PSObject -Property @{ SubscriptionId = $subId; ResourceGroups = $allVmCollections }
$subList = New-Object System.Collections.ArrayList
$subList.Add($rgCollection) > $null
$subCollection = New-Object PSObject -Property @{ Subscriptions = $subList }
$outputSubCollection = $subCollection | ConvertTo-Json -depth 100


Out-File -FilePath .\$subId.json -InputObject $outputSubCollection
Write-Host("Completed")