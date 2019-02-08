#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
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
	Write-Host "This script will collect a list of virtual machines per Azure Resource Group"
	Write-Host "from a provided Azure Subscription. Output will be written to a file called"
	Write-Host "SUBSCRIPITON_ID.json and can then be used, if renamed to VMConfiguration.json"
	Write-Host "with the AzureVmStateChange.ps1 script to bulk change VM states."
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
Write-Host "Scanning subscription for resource groups, this could take a while....."

$resourceGroups = Get-AzureRmResourceGroup #| Where-Object {$_.ResourceGroupName -eq "dangtestdel"}

#####################################################
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################
$hardwareSKU = @{}
$totalVms=0
$runningVms=0
$stoppedVms=0
$deallocatedVms=0
$vmStopCollections = New-Object System.Collections.ArrayList
$vmStoppedCollections = New-Object System.Collections.ArrayList

foreach($group in $resourceGroups)
{
	$vms = Get-AzureRmVM -ResourceGroupName $group.ResourceGroupName
	
	$runningVmCollection = New-Object System.Collections.ArrayList
	$stoppedVmCollection = New-Object System.Collections.ArrayList

	Write-Host("Collect VMS in resource group " + $group.ResourceGroupName)
	foreach($vminst in $vms)
	{
		$totalVms++
		$vmStatus = Get-AzureRmVM -ErrorAction Stop -Status -ResourceGroupName $group.ResourceGroupName -Name $vminst.Name
		if($vmStatus)
		{
			$running=$false
			$deallocated=$false
			foreach($status in $vmStatus.Statuses)
			{
				#Write-Host($status.code)
				if($status.code -eq "PowerState/running")
				{
					$runningVmCollection.Add($vminst.Name) > $null
					$running=$true
					break
				}
				if($status.code -eq "PowerState/deallocated")
				{
					$deallocated=$true
					break
				}
			}


			if($hardwareSKU.ContainsKey($vminst.Location) -eq $false)
			{
				$deallocatedList = @{}
				$runningList = @{}
				$stoppedList = @{}
				$locationSku = @{}
				$locationSku.Add("running", $runningList)
				$locationSku.Add("stopped", $stoppedList)
				$locationSku.Add("deallocated", $deallocatedList)
				$hardwareSKU.Add($vminst.Location,$locationSku)
			}
			
			$listIndex = "stopped"
			if($running)
			{
				$runningVms++
				$listIndex = "running"
			}
			elseif($deallocated)
			{
				$deallocatedVms++
				$listIndex = "deallocated"
			}
			else
			{
				$stoppedVmCollection.Add($vminst.Name) > $null
				$stoppedVms++
			}
			
			if($hardwareSKU[$vminst.Location][$listIndex].ContainsKey($vminst.HardwareProfile.VmSize) -eq $false)
			{
				$hardwareSKU[$vminst.Location][$listIndex].Add($vminst.HardwareProfile.VmSize,1)
			}
			else
			{
				$hardwareSKU[$vminst.Location][$listIndex][$vminst.HardwareProfile.VmSize]++
			}
		}
	}
	
	if($runningVmCollection.Count -gt 0)
	{
		$vmgroup = New-Object PSObject -Property @{ Name = $group.ResourceGroupName; VirtualMachines = $runningVmCollection }
		$vmStopCollections.Add($vmgroup) > $null
	}
	if($stoppedVmCollection.Count -gt 0)
	{
		$vmstoppedgroup = New-Object PSObject -Property @{ Name = $group.ResourceGroupName; VirtualMachines = $stoppedVmCollection }
		$vmStoppedCollections.Add($vmstoppedgroup) > $null
	}
}

#Create the status object
$vmCollection = New-Object PSObject -Property @{ 
	TotalVMCount = $totalVms; 
	RunningVms = $runningVms; 
	StoppedVms = $stoppedVms; 
	DeallocatedVms = $deallocatedVms; 
	VirtualMachines = $hardwareSKU }

#Create the configuration object
$rgConfigCollection = New-Object PSObject -Property @{ SubscriptionId = $subId; ResourceGroups = $vmStopCollections }
$subList = New-Object System.Collections.ArrayList
$subList.Add($rgConfigCollection) > $null
$subCollection = New-Object PSObject -Property @{ Subscriptions = $subList }
$outputSubCollection = $subCollection | ConvertTo-Json -depth 100

$rgStoppedConfigCollection = New-Object PSObject -Property @{ SubscriptionId = $subId; ResourceGroups = $vmStoppedCollections }
$stoppedSubList = New-Object System.Collections.ArrayList
$stoppedSubList.Add($rgStoppedConfigCollection) > $null
$stoppedSubCollection = New-Object PSObject -Property @{ Subscriptions = $stoppedSubList }
$stoppedOutputSubCollection = $stoppedSubCollection | ConvertTo-Json -depth 100

#Create a directory
md -ErrorAction Ignore -Name $subId
#Write the configuration file
$configurationFilename = "vm_config.json"
Out-File -FilePath .\$subId\$configurationFilename -InputObject $outputSubCollection
Write-Host("Completed")	
	
#Write the status file
$fileContent = $vmCollection | ConvertTo-Json -depth 100
$filename = "vm_status.json"
Out-File -FilePath .\$subId\$filename -InputObject $fileContent
Write-Host("Completed")

#Write the stopped machines file
$stoppedfilename = "vm_stopped.json"
Out-File -FilePath .\$subId\$stoppedfilename -InputObject $stoppedOutputSubCollection
Write-Host("Completed")

