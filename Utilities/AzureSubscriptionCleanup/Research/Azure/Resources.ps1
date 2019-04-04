###############################################################
# Resources.ps1
#
#	Contains scripts to work on individual resources
#
#
#	FUNCTIONS AVAILABLE
#			# List all resources in a subscription
#			GetResources -subId
#			# Find deployments of specific resource types
#			FindDeployments -subId -resourceType
#			# Find unmanaged Disks (which can be deleted)
#			FindUnmanagedDisks -subId -resourceGroup -unlockedOnly
#			# Delete Azure Disk
#			DeleteAzureDisk -subId -resourceGroup -diskName
###############################################################

. './Azure/ResourceGroup.ps1'

###############################################################
# FindUnleasedVhdFiles
#
#	Scans through storage accounts looking for VHD files with not
#	lease on them. 
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		[resourceGroup] : Specific resource group, otherwise whole sub searched.
#		[unlockedOnly] : $true means get only those in unlocked RG's
#
#	Returns:
#		Array<CloudBlob> 
#			suitable for value into  Remove-AzureStorageBlob -CloudBlob XX -Force
###############################################################
function FindUnleasedVhdFiles {
	Param ([string]$subId, [string]$resourceGroup, [switch]$unlockedOnly)

	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	
	$storageAccounts=$null
	if($resourceGroup)
	{
		$storageAccounts = Get-AzureRmStorageAccount -ResourceGroupName $name
	}
	else
	{
		# This fails horribly for our team sub:
		#Get-AzureRmStorageAccount : Unable to deserialize the response.
		#At line:1 char:1
		#+ Get-AzureRmStorageAccount
		#+ ~~~~~~~~~~~~~~~~~~~~~~~~~
		#    + CategoryInfo          : CloseError: (:) [Get-AzureRmStorageAccount], SerializationException
		#    + FullyQualifiedErrorId : Microsoft.Azure.Commands.Management.Storage.GetAzureStorageAccountCommand
		$storageAccounts = Get-AzureRmStorageAccount
	}
	
	$foundResourceGroups=@{}
	$unleasedVHDBlobs = New-Object System.Collections.ArrayList
	
	foreach($storageAccount in $storageAccounts){
		if($unlockedOnly)
		{
			# Get info for each resource group only once. 
			if($foundResourceGroups.ContainsKey($storageAccount.ResourceGroupName) -eq $false)
			{
				$details = LoadDetailedResourceGroup -resourceGroup $storageAccount.ResourceGroupName
				$foundResourceGroups.Add($storageAccount.ResourceGroupName, $details.Locks)
			}
			else
			{
				Write-Host("Been here before")
			}
			
			if($foundResourceGroups[$storageAccount.ResourceGroupName].Locks)
			{
				continue
			}
		}
		
		$storageKey = (Get-AzureRmStorageAccountKey -ResourceGroupName $storageAccount.ResourceGroupName -Name $storageAccount.StorageAccountName)[0].Value
		$context = New-AzureStorageContext -StorageAccountName $storageAccount.StorageAccountName -StorageAccountKey $storageKey
		$containers = Get-AzureRmStorageContainer -StorageAccount $storageAccount
		foreach($container in $containers){
			$blobs = Get-AzureStorageBlob -Container $container.Name -Context $context
			
			#Fetch all the Page blobs with extension .vhd as only Page blobs can be attached as disk to Azure VMs
			$blobs | Where-Object {$_.BlobType -eq 'PageBlob' -and $_.Name.EndsWith('.vhd')} | ForEach-Object { 
				#If a Page blob is not attached as disk then LeaseStatus will be unlocked
				if($_.ICloudBlob.Properties.LeaseStatus -eq 'Unlocked'){
					$unleasedVHDBlobs.Add($_) > $null
				}
			}
		}
	}

	$unleasedVHDBlobs
}


###############################################################
# FindUnmanagedDisks
#
#	Get a list of unmanaged disks.
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		[resourceGroup] : Specific resource group, otherwise whole sub searched.
#		[unlockedOnly] : $true means get only those in unlocked RG's
#
#	Returns:
#		List<PObject>
#			[string]DiskName
#			[string]ResourceGroup
###############################################################
function FindUnmanagedDisks {
	Param ([string]$subId, [string]$resourceGroup, [switch]$unlockedOnly)

	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	
	$searchGroups = @{}
	
	# Collect all the disks
	if($resourceGroup)
	{
		Write-Host("Find Unmanaged Disks in group: " + $resourceGroup)
		$disks = Get-AzureRmDisk -ResourceGroupName $resourceGroup
		foreach($disk in $disks)
		{
			if($disk.ManagedBy -eq $null)
			{
				$searchGroups.Add($disk.Name, $disk.ResourceGroupName)
			}
		}
	}
	else
	{
		Write-Host("Find Unmanaged Disks in subscription")
		$disks = Get-AzureRmDisk
		foreach($disk in $disks)
		{
			if($disk.ManagedBy -eq $null)
			{
				$searchGroups.Add($disk.Name, $disk.ResourceGroupName)
			}
		}
	}
	
	# Now loop through them
	$returnGroups = New-Object System.Collections.ArrayList
	foreach($id in $searchGroups.Keys)
	{
		$deleteOk=$true
		if($unlockedOnly)
		{
			$details = LoadDetailedResourceGroup -resourceGroup $searchGroups[$id]
			if($details.Locks)
			{
				$deleteOk = $false
			}
		}
		
		if($deleteOk)
		{
			$diskInfo = New-Object PSObject -Property @{ 
				DiskName = $id;
				ResourceGroup= $searchGroups[$id]}
			$returnGroups.Add($diskInfo) > $null
		}
	}

	$returnGroups
}

###############################################################
# DeleteAzureDisk
#
#	Delete a disk in Azure. Should use FindUnmanagedDisks to get
#	the list to work on. 
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		resourceGroup : Group disk lives in
#		diskName : Name of disk to delete
#
#	Returns:
#		Result of Remove-AzureRmDisk
###############################################################
function DeleteAzureDisk {
	Param ([string]$subId, [string]$resourceGroup, [string]$diskName)

	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	Remove-AzureRmDisk -ResourceGroupName $resourceGroup -DiskName $diskName -Force
}


###############################################################
# GetResources
#
#	Get a list of all resources and the count of each resource
#	type in a subscription. 
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#
#	Returns:
#		HashTable<[string]resourceType, [int]resourceCount>
###############################################################
function GetResources {
	Param ([string]$subId)

	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}

	$returnTable = @{}
	
	$allResources = Get-AzureRmResource
	foreach($res in $allResources)
	{
		if($returnTable.ContainsKey($res.ResourceType) -eq $false)
		{
			$returnTable.Add($res.ResourceType,1)
		}
		else
		{
			$returnTable[$res.ResourceType]++
		}
	}
	
	$returnTable
}

###############################################################
# FindDeployments
#
#	Find the deploymnets of a specific resource type in an AzureRmContext
#	subscription.
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		resourceType : String resource type to find, i.e. 
#				-resourceType 'Microsoft.MachineLearningServices/workspaces'
#
#	Returns:
#		HashTable<[string]resource group, HashTable2>
#			HashTable2<[string]resourceName, [string]resourceType
###############################################################
function FindDeployments {
	Param ([string]$subId,[string]$resourceType)

	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}

	# <rgname, hastable> , <hastable> = < name, type>
	$resourceListDictionary = @{}
	
	$resources = Get-AzureRmResource -ResourceType $resourceType
	foreach($res in $resources)
	{
		if($resourceListDictionary.ContainsKey($res.ResourceGroupName) -eq $false)
		{
			$resourceListDictionary.Add($res.ResourceGroupName,@{})
		}
		
		$resourceListDictionary[$res.ResourceGroupName].Add($res.Name, $resourceType)
	}
	
	$resourceListDictionary
}

