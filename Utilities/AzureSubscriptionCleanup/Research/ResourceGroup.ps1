###############################################################
# ResourceGroup.ps1
#
#	Contains scripts to collect or modify Azure Subscriptions. 
#
#
#	FUNCTIONS AVAILABLE
#			# Get a list of resource groups in a subscription 
#			GetResourceGroupInfo [-subId]
#			# Summarize the list of resource groups
#			GetResourceGroupSummary [-subId] [-history]
#			# Determine if a group is older than 60 days
#			ResourceGroupOlderThan60Days -groupName
#			# Parse the tags from a resource group
#			ParseTags -tags
#			# Parses the locks from a resource group
#			ParseLocks -locks
#			# Check existence of tags 
#			FindMissingTags -tags -expected
#			# Modify group tags
#			ModifyGroupTags [-subId] -groupName -tags
#			# Remove resource locks on group
#			UnlockGroup [-subId] -groupName
#			# Is name a default Azure RG
#			IsSpecialGroup  -groupName
#			# Get groups in buckets
#			GetResourceGroupBuckets [-subId]
#		
###############################################################



###############################################################
# GetResourceGroupSummary
#
#	Collects a summary of resource groups in the subscription.
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		groups : Output of GetResourceGroupInfo
#		history : Flag, if present looks for >60 days, careful
#				  it's slow. Default is NOT to look.
#
#	Returns:
#		PSObject
#			Total - int
#			OlderThan60 - int  (-1 if flag is missing)
#			DeleteLocked - int 
#			ReadOnlyLocked - int 
#			Special - int 
#			Regions - Hashtable, key=region, value=# in region.
###############################################################
function GetResourceGroupSummary{
	Param ([string]$subId, $groups, [switch]$history)

	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	
	$totalResourceGroups = 0
	$lockedRG = 0
	$readOnlyRG = 0
	$specialRG = 0
	$oldRG = 0
	$regions=@{}

	foreach ($g in $groups.GetEnumerator()) {
	
		$totalResourceGroups += 1
		
		# Determine Lock State and add to count
		$locks = ParseLocks -locks $g.Value.Locks
		foreach($lockName in $locks.Keys)
		{
			if($locks[$lockName] -like "CanNotDelete")
			{
				$lockedRG+=1
			}
			if($locks[$lockName] -like "ReadOnly")
			{
				$readOnlyRG+=1
			}
		}
		
		# Determine if it's specialRG
		$special = IsSpecialGroup -groupName $g.Value.Name
		if($special -eq $true)
		{
			$specialRG += 1
		}
		
		# Get the region
		if($regions.ContainsKey($g.Value.Location) -eq $false)
		{
			$regions.Add($g.Value.Location,0)
		}
		$regions[$g.Value.Location] += 1
		
		if($history)
		{
			# is it old?
			$old = ResourceGroupOlderThan60Days -groupName $g.Value.Name
			if($old)
			{
				$oldRG += 1
			}
		}
		else
		{
			$oldRG =-1
		}
	}
	
	$summary = New-Object PSObject -Property @{ 
			Total = $totalResourceGroups;
			OlderThan60= $oldRG;
			DeleteLocked = $lockedRG; 
			ReadOnlyLocked=$readOnlyRG;
			Special=$specialRG;
			Regions=$regions}
			
	$summary
}

###############################################################
# ResourceGroupOlderThan60Days
#
#	Determine if a resource group is >60 days old. Expects that 
#	the context has been set to the right subscription.
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		groupName : Resource Group Name
#
#	Returns:
#		Boolean flag, $true is >60 days
###############################################################
function ResourceGroupOlderThan60Days {
	Param ([string]$subId, [string]$groupName)
	
	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}
	
	Write-Host("Checking age of " + $groupName)
	
	$returnValue = $false
	$resources = Get-AzureRmResource -ResourceGroupName $groupName
	
	$pointInTime = [DateTime]::Now.AddDays(-60)
	$horizon = $pointInTime.AddDays(-15)

	foreach($res in $resources)
	{
		$logs = Get-AzureRmLog -StartTime $horizon -EndTime $pointInTime -Status "Succeeded" -ResourceId $res.ResourceId -WarningAction "SilentlyContinue" `
		
		if($logs.Count -gt 0)
		{
			$returnValue = $true
			break
		}
	}

	$returnValue
}

###############################################################
# GetResourceGroupInfo
#
#	Get a list of all resource groups in a subscription with 
#	lock and tag information. 
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#
#	Returns:
#		Hashtable<[string]subname, [object]info>
#			Name - String
#			Location - String  
#			Locks - PSObject 
#			Tags - PSObject
###############################################################
function GetResourceGroupInfo {
	Param ([string]$subId)

	$returnTable = @{}
	
	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}

	$resourceGroups = Get-AzureRmResourceGroup

	#####################################################
	# Go through each one looking at locks. Add them to
	# the appropriate list - locked unlocked.
	#####################################################
	foreach($group in $resourceGroups)
	{
		Write-Host("Checking " + $group.ResourceGroupName + " L:" + $group.Location)
		$locks = Get-AzureRmResourceLock -ResourceGroupName $group.ResourceGroupName
		
		$rgInformation = New-Object PSObject -Property @{ 
			Name = $group.ResourceGroupName;
			Locks = $locks; 
			Tags=$group.Tags;
			Location=$group.Location}
		$returnTable.Add($group.ResourceGroupName,$rgInformation)
	}

	$returnTable
}

###############################################################
# GetResourceGroupBuckets
#
#	Get Resource groups in 4 buckets
#		DeleteLocked, ReadOnlyLocked, Unlocked, Special
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#
#	Returns:
#		Hashtable<[string]bucketName, [list]groupNames>
###############################################################
function GetResourceGroupBuckets{
	Param ([string]$subId)

	$deleteKey = "DeleteLocked"
	$readOnlyKey = "ReadOnlyLocked"
	$unlockedKey = "Unlocked"
	$specialKey = "Special"
	
	$returnTable = @{}
	$returnTable.Add($deleteKey,(New-Object System.Collections.ArrayList))
	$returnTable.Add($readOnlyKey,(New-Object System.Collections.ArrayList))
	$returnTable.Add($unlockedKey,(New-Object System.Collections.ArrayList))
	$returnTable.Add($specialKey, (New-Object System.Collections.ArrayList))
	
	$groups = GetResourceGroupInfo -subId $subId
	foreach($group in $groups.GetEnumerator())
	{
		# Check Special First
		$special = IsSpecialGroup -groupName $group.Value.Name
		if($special -eq $true)
		{
			$returnTable[$specialKey].Add($group.Value.Name) > $null
			continue
		}
		
		$lockCounts=0
		$locks = ParseLocks -locks $group.Value.Locks
		foreach($lockName in $locks.Keys)
		{	
			$lockCounts += 1
			if($locks[$lockName] -like "CanNotDelete")
			{
				$returnTable[$deleteKey].Add($group.Value.Name) > $null
			}
			elseif($locks[$lockName] -like "ReadOnly")
			{
				$returnTable[$readOnlyKey].Add($group.Value.Name) > $null
			}
		}
		
		if($lockCounts -eq 0)
		{
			$returnTable[$unlockedKey].Add($group.Value.Name) > $null
		}
	}
	
	$returnTable
}

###############################################################
# IsSpecialGroup
#
#	Determine if the name is an Azure Default
#
#	Params:
#		groupName : Name of a group
#
#	Returns:
#		$true if special, $false otherwise
###############################################################
function IsSpecialGroup{
	Param ([string]$groupName)
	
	$special = $false
	if($groupName.Contains("cleanup") -or
		   $groupName.Contains("Default-Storage-") -or
		   ( $groupName.Contains("DefaultResourceGroup-") -or
			 $groupName.Contains("Default-MachineLearning-") -or
			 $groupName.Contains("cloud-shell-storage-") -or
			 $groupName.Contains("Default-ServiceBus-") -or
			 $groupName.Contains("Default-Web-") -or
			 $groupName.Contains("OI-Default-") -or
			 $groupName.Contains("Default-SQL") -or
			 $groupName.Contains("StreamAnalytics-Default-") -or
			 $groupName.Contains("databricks-")
			)
		  )
	{
		$special = $true
	}
	
	$special
}

###############################################################
# ParseTags
#
#	Parse the tags associated with an Azure Resource. When 
#	acquired by Azure CLI it is a hashtable with name/value.
#
#	Params:
#		tags : Tags hash table associated with a resource group
#
#	Returns:
#		Hashtable<[string]tagname, [string]tagvalue>
###############################################################
function ParseTags {
	Param ($tags)

	$returnTable = @{}
	
	if($tags)
	{
		foreach($key in $tags.Keys)
		{
			$returnTable.Add($key, $tags[$key])
		}
	}
	$returnTable
}

###############################################################
# ParseLocks
#
#	Parse the locks associated with a resource group. The locks
#	are acquired through the Azure CLI and contain a list of 
#	PSObjects.
#
#	Params:
#		locks : Locks associated with resource group
#
#	Returns:
#		Hashtable<[string]lockname, [string]locktype>
###############################################################
function ParseLocks {
	Param ($locks)
	
	$returnTable = @{}
	
	if($locks)
	{
		$foundlocks = New-Object System.Collections.ArrayList
		if($locks.Length -gt 0)
		{
			foreach($lock in $locks)
			{	
				$foundlocks.Add($lock) > $null
			}
		}
		else
		{
			$foundlocks.Add($locks) > $null
		}
	
		# It has a lock either ReadOnly or CanNotDelete so it has to 
		# be marked as locked.
		foreach($lock in $foundlocks)
		{
			$properties = $lock.Properties | ConvertTo-Json
			$propobject = ConvertFrom-Json -InputObject $properties
			$lockType = $propobject.psobject.properties["level"].value
			$returnTable.Add($lock.Name, $lockType)
		}
	}
	
	$returnTable
}	

###############################################################
# FindMissingTags
#
#	Searches existing group tags for expected tags. Returns list
#	of expected tags that are not present.
#
#	Params:
#		tags : Tags hash table associated with a resource group
#			   obtained by calling ParseTags
#		expected : List of tag names expected to be found
#
#	Returns:
#		List<missing expected tag names>
###############################################################
function FindMissingTags {
	Param($tags, $expected)
	$returnTable = New-Object System.Collections.ArrayList
	
	if($tags)
	{
		foreach($etag in $expected)
		{
			if($tags.ContainsKey($etag) -eq $false)
			{
				$returnTable.Add($etag) > $null
			}
		}
	}
	else
	{
		$returnTable = $expected
	}
	
	$returnTable
}

###############################################################
# ModifyGroupTags
#
#	Modify the tags on a resource group.
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		groupName: Resource group to modify. 
#		tags : Hash table of tagname, tagvalue. If null
#						clears all tags.
#	Returns:
#		Output of az group update --tags
###############################################################
function ModifyGroupTags{
	Param($subId, $groupName, $tags)
	
	Write-Host("Setting subscription")
	if($subId)
	{
		$context = az account set -s $subId
	}

	$groupObject = (az group show -g $groupName) | ConvertFrom-Json
	$tagsList = New-Object System.Collections.ArrayList
		
	if($groupObject.Tags)
	{
		Write-Host("Obtaining existing tags ...")
		$groupObject.Tags.PSObject.Properties | Foreach { $tagsList.Add($_.Name +"=" + "'" + $_.Value +"'") > $null }
	}
	
	Write-Host("Format command input")
	$tagInput = $null
	if($tags)
	{
		foreach($key in $tags.Keys)
		{
			$tagsList.Add($key + "='" + $tags[$key] + "'") > $null
		}

		Write-Host("Adding tags....")
		foreach($tag in $tagsList)
		{	
			$tagInput += " " + $tag
		}
	}
	else
	{
		Write-Host("Adding tags....")
		$tagInput += "''"
	}

	# Create the command string and execute it.
	Write-Host("Updating " + $groupName + " with new tag list " + $tagInput)
	$commandString = "az group update -n " + $groupName + " --tags " + $tagInput
	Invoke-Expression $commandString
}

###############################################################
# UnlockGroup
#
#	Remove resource locks on a resource group.
#
#	Params:
#		[subId] : Subscription to work on. If present context switched.
#		groupName: Resource group to modify. 
#						clears all tags.
#	Returns:
#		Count of locks removed
###############################################################
function UnlockGroup {
	Param($subId, $groupName)

	$removalCount=0
	
	if($subId)
	{
		$context = Set-AzureRmContext -SubscriptionID $subId
	}

	$locks = Get-AzureRmResourceLock -ResourceGroupName $groupName
	if($locks)
	{
		# It has a lock either ReadOnly or CanNotDelete so it has to 
		# be marked as locked.
		foreach($lock in $locks)
		{
			$removalCount += 1
			Write-Host("Removing lock : " + $lock.LockId + " from " + $rg.ResourceGroupName)
			$result = Remove-AzureRmResourceLock -Force -LockId $lock.LockId
		}
	}
	else
	{
		Write-Host("No locks to delete")
	}
	
	$removalCount
}