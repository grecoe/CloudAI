#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
# whatif - Run it as if we would delete but only display
#		   what would happen if executed.
#####################################################
param(
	[string]$subId,
	[switch]$help=$false,
	[switch]$login=$false
)

#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host "This script will delete resource groups from an Azure Subscription."
	Write-Host "You will be prompted to log in but MUST provide a subscription ID."
	Write-Host "Parameters:"
	Write-Host "	-help : Shows this help message"
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-login : Tells script to log into azure subscription, otherwise assumes logged in already"
	break
}

Set-StrictMode -Version 1

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
Write-Host "Collect information about the subscription....."

$totalResourceGroups = 0
$specialResourceGroups = 0
$oldResourceGroups = 0
$resourceGroupRegions = @{}

$totalVirtualMachines=0
$runningVirtualMachines=0
$stoppedVirtualMachines=0
$deallocatedVirtualMachines=0
$virtualMachineSkus = @{}

$resourceList = @{}

$resourceGroups = Get-AzureRmResourceGroup #| Where-Object {$_.ResourceGroupName -eq "dangtestdel"}

$resourceLogs = New-Object System.Collections.ArrayList
#####################################################
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################
foreach($group in $resourceGroups)
{
	$totalResourceGroups++
	
	$name=$group.ResourceGroupName

	# Keep track of specials, these are created by other services and 
	# are not something generally in control of a user.
	if(	$name.Contains("cleanup") -or
		$name.Contains("Default-Storage-") -or
		$name.Contains("DefaultResourceGroup-") -or
		$name.Contains("Default-MachineLearning-") -or
		$name.Contains("cloud-shell-storage-") -or
		$name.Contains("Default-ServiceBus-") -or
		$name.Contains("Default-Web-") -or
		$name.Contains("OI-Default-") -or
		$name.Contains("Default-SQL") -or
		$name.Contains("StreamAnalytics-Default-")
	  )
	{
		$specialResourceGroups++
	}

	# Find the region the RG lives in
	if($resourceGroupRegions.ContainsKey($group.Location) -eq $false)
	{
		$resourceGroupRegions.Add($group.Location,1)
	}
	else
	{
		$resourceGroupRegions[$group.Location]++
	}
	
	# Determine if it's older than 60 days
	# $groupResources = Find-AzureRmResource -ResourceGroupNameEquals $name
	$groupResources = Get-AzureRmResource -ResourceGroupName $name
	
	$pointInTime = [DateTime]::Now.AddDays(-60)
	$horizon = $pointInTime.AddDays(-29)

	foreach($groupResource in $groupResources)
	{
		$resourceLogs = Get-AzureRmLog -StartTime $horizon -EndTime $pointInTime -Status "Succeeded" -ResourceId $groupResource.ResourceId -WarningAction "SilentlyContinue" 
		if($resourceLogs -and $resourceLogs.Count -gt 0)
		{
			$oldResourceGroups++
			break
		}
	}
	
	# Get Virtual Machine Stats
	$vms = Get-AzureRmVM -ResourceGroupName $name
	foreach($vminst in $vms)
	{
		$totalVirtualMachines++
		$vmStatus = Get-AzureRmVM -ErrorAction Stop -Status -ResourceGroupName $group.ResourceGroupName -Name $vminst.Name
		if($vmStatus)
		{
			# Get the state of the VM
			$running=$false
			$deallocated=$false
			foreach($status in $vmStatus.Statuses)
			{
				if($status.code -eq "PowerState/running")
				{
					$running=$true
					break
				}
				if($status.code -eq "PowerState/deallocated")
				{
					$deallocated=$true
					break
				}
			}
			
			# Record the state count to the total.
			if($running)
			{
				$runningVirtualMachines++
			}
			elseif($deallocated)
			{
				$deallocatedVirtualMachines++
			}
			else
			{
				$stoppedVirtualMachines++
			}

			# Capture the size
			if($virtualMachineSkus.ContainsKey($vminst.HardwareProfile.VmSize) -eq $false)
			{
				$virtualMachineSkus.Add($vminst.HardwareProfile.VmSize,1)
			}
			else
			{
				$virtualMachineSkus[$vminst.HardwareProfile.VmSize]++
			}
		}
	}	

	if(($totalResourceGroups % 20) -eq 0)
	{
		Write-Host "Still scanning resource groups..."
	}
}

$allResources = Get-AzureRmResource
foreach($res in $allResources)
{
	if($resourceList.ContainsKey($res.ResourceType) -eq $false)
	{
		$resourceList.Add($res.ResourceType,1)
	}
	else
	{
		$resourceList[$res.ResourceType]++
	}
}

#Create a directory
md -ErrorAction Ignore -Name $subId

# Create and output object with subscription overview
$resourceGroupStats = New-Object PSObject -Property @{ 
	Total = $totalResourceGroups; 
	Specials = $specialResourceGroups;
	Older60Days = $oldResourceGroups;
	Regions = $resourceGroupRegions}
	
$virtualMachineStats = New-Object PSObject -Property @{ 
	Total = $totalVirtualMachines; 
	Running = $runningVirtualMachines; 
	Stopped = $stoppedVirtualMachines;
	Deallocated = $deallocatedVirtualMachines}
	
$subscriptionOverview = New-Object PSObject -Property @{ 
	ResourceGroups = $resourceGroupStats; 
	VirtualMachines = $virtualMachineStats;
	ResourceUsage = $resourceList}
	
Out-File -FilePath .\$subId\subscription_overview.json -InputObject ($subscriptionOverview | ConvertTo-Json -depth 100)

Write-Host("Completed")