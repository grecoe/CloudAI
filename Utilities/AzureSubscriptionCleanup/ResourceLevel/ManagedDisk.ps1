# This script will find, in unlocked resource groups, 
#	Managed Disks (attached to VM)
#	Unmanaged Disks
#	VHD files (disks) that have no lease on them
#
# The output goes to a JSON file in the directory that this script is run in.
# Optionally delete VHD and Unmanaged disks using flags
#
# Recommended: Delete VHD (-deleteVHD) when clearing storage accounts.
#
# If you are going to be deleting unlocked groups anyway, you can set both flags
# : -deleteVHD -deleteUnmanagedDisk
#
#
# Regardless of flag state, if -whatif is present, no deletes will occur.
#
# Default delete state (either) - FALSE



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
	[switch]$login=$false,
	[switch]$deleteVHD=$false,
	[switch]$deleteUnmanagedDisk=$false,
	[switch]$whatif=$false
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
	Write-Host "	-deleteVHD : Tells script delete unleased VHD files in unlocked resource groups"
	Write-Host "	-deleteUnmanagedDisk : Tells script to delete any unmanaged disks (different than above)"
	Write-Host "	-whatif : Regardless of other flags, nothing will be deleted"
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
# What-if forces delete flags to false
#####################################################
if($whatif -eq $true)
{
	$deleteVHD=$false
	$deleteUnmanagedDisk=$false
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

$foundUnmanagedDisks=@{}
$foundManagedDisks=@{}
$foundUnLeasedVHDFiles=@{}
$foundLeasedVHDFiles=@{}

$resourceGroups = Get-AzureRmResourceGroup 

#####################################################
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################
foreach($group in $resourceGroups)
{
	$name=$group.ResourceGroupName

	$locks = Get-AzureRmResourceLock -ResourceGroupName $name
	if($locks)
	{
		Write-Host("Group Locked, bypassing : " + $name)
	}
	else
	{
		Write-Host("Checking Unlocked Resource Group: " + $name)
		
		# Check the traditional getting RmDisk
		$managedDisks = Get-AzureRmDisk -ResourceGroupName $name
		foreach ($md in $managedDisks) {
			
			# ManagedBy property stores the Id of the VM to which Managed Disk is attached to
			# If ManagedBy property is $null then it means that the Managed Disk is not attached to a VM
			if($md.ManagedBy -eq $null){
				#Write-Host "Unmanaged disk : " + $md.Name
				if($foundManagedDisks.ContainsKey($name) -eq $false)
				{
					$foundManagedDisks.Add($name, (New-Object System.Collections.ArrayList))
				}
				$coll = New-Object System.Collections.ArrayList
				$coll.Add($md.Name) > $null
				$coll.Add($md.Id) > $null
				$foundManagedDisks[$name].Add($coll) > $null
			}
			else
			{
				#Write-Host "Managed Disk" + $md.Name
				if($foundUnmanagedDisks.ContainsKey($name) -eq $false)
				{
					$foundUnmanagedDisks.Add($name, (New-Object System.Collections.ArrayList))
				}
				$coll = New-Object System.Collections.ArrayList
				$coll.Add($md.Name) > $null
				$coll.Add($md.Id) > $null
				$foundUnmanagedDisks[$name].Add($coll) > $null
				
				if($deleteUnmanagedDisk -eq $true)
				{
					Write-Host("Deleting unmanaged disk")
					Remove-AzureRmDisk -ResourceGroupName $name -DiskName $md.Name -Force
				}
			}
		}
		
		#Now scan the storage accounts and see if there is a difference
		$storageAccounts = Get-AzureRmStorageAccount -ResourceGroupName $name
		foreach($storageAccount in $storageAccounts){
			$storageKey = (Get-AzureRmStorageAccountKey -ResourceGroupName $storageAccount.ResourceGroupName -Name $storageAccount.StorageAccountName)[0].Value
			$context = New-AzureStorageContext -StorageAccountName $storageAccount.StorageAccountName -StorageAccountKey $storageKey
			#$containers = Get-AzureRmStorageContainer -Context $context
			$containers = Get-AzureRmStorageContainer -StorageAccount $storageAccount
			foreach($container in $containers){
				$blobs = Get-AzureStorageBlob -Container $container.Name -Context $context
				#Fetch all the Page blobs with extension .vhd as only Page blobs can be attached as disk to Azure VMs
				$blobs | Where-Object {$_.BlobType -eq 'PageBlob' -and $_.Name.EndsWith('.vhd')} | ForEach-Object { 
					#If a Page blob is not attached as disk then LeaseStatus will be unlocked
					if($_.ICloudBlob.Properties.LeaseStatus -eq 'Unlocked'){
						if($foundUnLeasedVHDFiles.ContainsKey($name) -eq $false)
						{
							$foundUnLeasedVHDFiles.Add($name, (New-Object System.Collections.ArrayList))
						}
						$coll = New-Object System.Collections.ArrayList
						$coll.Add($_.ICloudBlob.Name) > $null
						$coll.Add($_.ICloudBlob.Uri.AbsoluteUri) > $null
						$coll.Add($storageAccount.StorageAccountName) > $null
						$foundUnLeasedVHDFiles[$name].Add($coll) > $null
						
						if($deleteVHD -eq $true)
						{
							Write-Host("Deleting un-leased VHD blob")
							$_ | Remove-AzureStorageBlob -Force
						}
					}
					else
					{
						if($foundLeasedVHDFiles.ContainsKey($name) -eq $false)
						{
							$foundLeasedVHDFiles.Add($name, (New-Object System.Collections.ArrayList))
						}
						$coll = New-Object System.Collections.ArrayList
						$coll.Add($_.ICloudBlob.Name) > $null
						$coll.Add($_.ICloudBlob.Uri.AbsoluteUri) > $null
						$coll.Add($storageAccount.StorageAccountName) > $null
						$foundLeasedVHDFiles[$name].Add($coll) > $null
					}
				}
			}
		}		
	}
}

$diskStatus = New-Object PSObject -Property @{ 
	ManagedDisks = $foundManagedDisks; 
	UnmanagedDisks = $foundUnmanagedDisks;
	UnleasedVHDFile = $foundUnLeasedVHDFiles;
	LeasedVHDFile=$foundLeasedVHDFiles}
	
$content = $diskStatus | ConvertTo-Json -depth 100
Out-File -FilePath .\azuredisks.json -InputObject $content
Write-Host("Completed")

 
