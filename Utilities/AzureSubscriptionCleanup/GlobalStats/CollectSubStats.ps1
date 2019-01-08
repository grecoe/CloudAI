#####################################################
# It's expected that the file subscriptionlist.json
# Exists in this directory. It would have been created
# by the script GetSubscription.ps1 and is actually 
# called from there (but could be called standalone.
#####################################################
$resconstitutedSubs = Get-Content -Raw -Path .\subscriptionlist.json | ConvertFrom-Json

#####################################################
# Arrays that hold information on the roll up of 
# resource groups and virtual machines.
#####################################################
$vmList = New-Object System.Collections.ArrayList
$rgList = New-Object System.Collections.ArrayList
$resourceList = @{}

#####################################################
# Variables to hold global stats from all of the 
# subscriptions.
#####################################################
$totalSubCount=0
$totalRgCount=0
$totalUnlockedRgCount=0
$totalVmCount=0
$totalRunningVmCount=0
$resourceGroupList = New-Object System.Collections.ArrayList

#####################################################
# Go through each of the subscription data that was
# collected by running GetSubscription.ps1
#####################################################
foreach($rsub in $resconstitutedSubs)
{
	$totalSubCount++
		
	Write-Host($rsub.Name)
	
	$vmfile = (Split-Path $MyInvocation.MyCommand.Path) + "\" + $rsub.Id + "\vm_status.json"
	$rgfile = (Split-Path $MyInvocation.MyCommand.Path) + "\" + $rsub.Id + "\rg_status.json"
	$rsfile = (Split-Path $MyInvocation.MyCommand.Path) + "\" + $rsub.Id + "\list_resources.json"
		
	#####################################################
	# Proceed only if files for this subscription were 
	# actually created.
	#####################################################
	if([System.IO.File]::Exists($vmfile) -and 
		[System.IO.File]::Exists($rgfile) -and
		[System.IO.File]::Exists($rsfile))
	{

		# Convert file content to an object
		$vmInfo = Get-Content -Raw -Path $vmfile | ConvertFrom-Json
		$rgInfo = Get-Content -Raw -Path $rgfile | ConvertFrom-Json
		$rsInfo = Get-Content -Raw -Path $rsfile | ConvertFrom-Json

		#####################################################
		# Collect the resource group stats
		#####################################################
		$rgStats = New-Object PSObject -Property @{ 
			Sub = $rsub.Name;
			Id = $rsub.Id;
			Total = $rgInfo.Total; 
			Unlocked = $rgInfo.Unlocked}
		$rgList.Add($rgStats) > $null
		
		$totalRgCount += $rgInfo.Total
		$totalUnlockedRgCount += $rgInfo.Unlocked
		
		#####################################################
		# Collect the virtual machine stats
		#####################################################
		$vmStats = New-Object PSObject -Property @{ 
			Sub = $rsub.Name;
			Id = $rsub.Id;
			Total = $vmInfo.TotalVMCount; 
			Running = $vmInfo.RunningVms;
			Stopped = $vmInfo.StoppedVms}
		$vmList.Add($vmStats) > $null

		$totalVmCount += $vmInfo.TotalVMCount
		$totalRunningVmCount += $vmInfo.RunningVms

		#####################################################
		# Collect the resource usage
		#####################################################
		foreach($regions in $rsInfo.PsObject.Properties)
		{
			foreach($region_details in $regions.PsObject.Properties)
			{	
				if($region_details.Name -eq "Value")
				{
					foreach($resourceType in $region_details.Value.PsObject.Properties)
					{
						if($resourceList.ContainsKey($resourceType.Name) -eq $false)
						{
							$resourceList.Add($resourceType.Name,0)
						}
						$resourceList[$resourceType.Name] += $resourceType.Value
					}
				}
			}
		}

		
		# If there is more than 0 resource groups associated with the subscription
		# collect it's the stats specifically for this subscription
		if($rgInfo.Total -gt 0)
		{
			$individualSubStatsRollup = New-Object PSObject -Property @{ 
				Subscription = $rsub.Name;
				ResourceGroups = $rgInfo.Total;
				UnlockedResourceGroups = $rgInfo.Unlocked;
				VirtualMachines = $vmInfo.TotalVMCount; 
				RunningVirtualMachines = $vmInfo.RunningVms}
			$resourceGroupList.Add($individualSubStatsRollup) > $null
		}
	}
	else
	{
		Write-Host("Not all info available for " + $rsub.Name)
	}
}


#####################################################
# Dump out the resource group,vm, and global resource usage information
#####################################################
Out-File -FilePath .\resource_group_status.json -InputObject ($rgList | ConvertTo-Json -depth 100)
Out-File -FilePath .\virtual_machine_status.json -InputObject ($vmList | ConvertTo-Json -depth 100)
Out-File -FilePath .\global_resource_usage.json -InputObject ($resourceList | ConvertTo-Json -depth 100)


#####################################################
# Dump out the global information
#####################################################
$glogalStats = New-Object PSObject -Property @{ 
	Subscriptions = $totalSubCount;
	ResourceGroups = $totalRgCount;
	ResourceGroupList = $resourceGroupList;
	UnlockedResourceGroups = $totalUnlockedRgCount;
	VirtualMachines = $totalVmCount; 
	RunningVirtualMachines = $totalRunningVmCount}
Out-File -FilePath .\subscription_global_stats.json -InputObject ($glogalStats | ConvertTo-Json -depth 100)
