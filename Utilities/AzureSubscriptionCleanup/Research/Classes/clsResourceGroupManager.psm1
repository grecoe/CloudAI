#############################################################################
#	When the user issues the call to ResourceGroupManager::GetGroupDetails
#	This is used to determine if a group is managed by another group.
#############################################################################
class GroupDetails {
	[string]$ManagedBy	
	[string]$ManagedByResourceGroup
	[System.Collections.HashTable]$Properties
	
	GroupDetails()
	{
		$this.Properties = New-Object System.Collections.HashTable
	}
}


#############################################################################
#	Class that contains information about an individual resource group 
#	with additional functionality to work with the group.
#		[System.Collections.ArrayList]FindMissingTags($expectedTagArray)
#		[void] ModifyTags($newTagHashTable)
#		[void] Unlock()
#		[void] Delete()
#		[bool] OlderThan60Days() WARNING this takes some time.
#############################################################################
class ResourceGroup{
    [string]$Id
    [string]$Name
    [string]$Location
    [System.Collections.HashTable]$Tags
    [System.Collections.HashTable]$Locks
	
	ResourceGroup(){
		$this.Tags = @{}
		$this.Locks = @{}
	}
	
	#########################################################################
	#	Compare tags against an array of tag names being passed in. Returns
	#	a list of expected tags that are not there.
	#########################################################################
	[System.Collections.ArrayList]FindMissingTags($expectedTagArray){
		$returnTable = New-Object System.Collections.ArrayList
		foreach($expectedTag in $expectedTagArray)
		{
			if($this.Tags.ContainsKey($expectedTag) -eq $false)
			{
				$returnTable.Add($expectedTag) > $null
			}
		}
		
		return $returnTable
	}
	
	#########################################################################
	#	Input is a hash table of tagName, tagValue. If empty, tags are removed
	#	from the resource group, otherwise the tags are appended to the already
	# 	present tags. 
	#########################################################################
	[void] ModifyTags($newTagHashTable)
	{
		$tagsList = New-Object System.Collections.ArrayList
		
		if($this.Tags.Count -gt 0)
		{
			Write-Host("Obtaining existing tags ...")
			$this.Tags.Keys | Foreach { $tagsList.Add($_ +"='" + $this.Tags[$_] +"'") > $null }
		}

		$tagInput = $null
		if($newTagHashTable)
		{
			$newTagHashTable.Keys | Foreach { $tagsList.Add($_ +"='" + $newTagHashTable[$_] +"'") > $null }

			foreach($tag in $tagsList)
			{	
				$tagInput += " " + $tag
			}
		}
		else
		{
			$tagInput += "''"
		}
		
		Write-Host("Updating " + $this.Name + " with new tag list " + $tagInput)
		$commandString = "az group update -n " + $this.Name + " --tags " + $tagInput
		Invoke-Expression $commandString
	}
	
	#########################################################################
	#	Remove any readonly or delete lock from a resource group.
	#########################################################################
	[void] Unlock(){
		if($this.Locks.Count -gt 0)
		{
			$rgLocks = Get-AzureRmResourceLock -ResourceGroupName $this.Name
			if($rgLocks)
			{
				foreach($lock in $rgLocks)
				{
					$result = Remove-AzureRmResourceLock -Force -LockId $lock.LockId
				}
			}
		}
	}
	
	#########################################################################
	#	Delete the resource group ONLY if it's unlocked.
	#########################################################################
	[void] Delete(){
	
		if($this.Locks.Count -eq 0)
		{
			Remove-AzureRmResourceGroup -Force -Name $this.Name
		}
	}
	
	#########################################################################
	#	Determine if a group is > 60 days old. 
	#########################################################################
	[bool] OlderThan60Days()
	{
		Write-Host("Checking age of " + $this.Name)
		
		$returnValue = $false
		$resources = Get-AzureRmResource -ResourceGroupName $this.Name
		
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
	
		return $returnValue
	}
}

#############################################################################
#	Returned from ResourceGroupManager::GetGroupBuckets
#############################################################################
class GroupBuckets{
    [System.Collections.ArrayList]$DeleteLocked
    [System.Collections.ArrayList]$ReadOnlyLocked
    [System.Collections.ArrayList]$Unlocked
    [System.Collections.ArrayList]$Special
	
	GroupBuckets(){
		$this.DeleteLocked = New-Object System.Collections.ArrayList
		$this.ReadOnlyLocked = New-Object System.Collections.ArrayList
		$this.Unlocked = New-Object System.Collections.ArrayList
		$this.Special = New-Object System.Collections.ArrayList
	}
}

#############################################################################
#	Returned from ResourceGroupManager::GetGroupSummary
#############################################################################
class GroupSummary{
	[int]$TotalGroups
	[int]$LockedGroups
	[int]$OldGroups
    [System.Collections.HashTable]$GroupDeployments
	
	GroupSummary(){
		$this.GroupDeployments = New-Object System.Collections.HashTable
	}
}

#############################################################################
#	Manager object to load and manage resource groups across a subscription.
#	This should be instantiated AFTER the subscription has been set. 
#		[ResourceGroup] GetGroup([String]$groupName)
#		[GroupDetails] GetGroupDetails([String]$groupName)
#		[GroupBuckets] GetGroupBuckets()
#############################################################################
class ResourceGroupManager {
	[System.Collections.ArrayList]$ResourceGroups=$null

	ResourceGroupManager()
	{
		$this.ClearCache()
	}
	
	###############################################################
	# Clear the internals if you switch subscriptions so that the
	# correct information will be returned.
	###############################################################
	[void] ClearCache() {
		$this.ResourceGroups = New-Object System.Collections.ArrayList
		$this.CollectResourceGroups()
	}

	#########################################################################
	#	Get an individual ResourceGroup based on name.
	#########################################################################
	[ResourceGroup] GetGroup([String]$groupName){
		$returnGroup = $null
		
		$groups = $this.ResourceGroups | Where-Object { $_.Name -eq $groupName}
		if($groups -and ($groups.Count -eq 1))
		{
			$returnGroup = $groups[0]
		}
		
		return $returnGroup
	}
	
	#########################################################################
	#	Get group(s) using a name pattern for -like, may return >1
	#########################################################################
	[System.Collections.ArrayList] FindGroup([String]$groupNamePattern){
		$returnGroup = New-Object System.Collections.ArrayList
		
		$groups = $this.ResourceGroups | Where-Object { $_.Name -like $groupNamePattern}
		foreach($group in $groups)
		{
			$returnGroup.Add($group)
		}
		
		return $returnGroup
	}
	
	#########################################################################
	#	Get more details on a resource group
	#########################################################################
	[GroupDetails] GetGroupDetails([String]$groupName){
		$returnDetails=[GroupDetails]::new()
		
		$rgObject = (az group show -g $groupName) | ConvertFrom-Json
		
		if($rgObject)
		{
			$managedByGroup=$null
			if($rgObject.managedBy)
			{
				$resourceObject = (az resource show --ids $rgObject.managedBy) | ConvertFrom-Json
				$managedByGroup = $resourceObject.resourceGroup
				
				$returnDetails.ManagedBy = $rgObject.managedBy
				$returnDetails.ManagedByResourceGroup = $managedByGroup
				
				$rgObject.Properties.PSObject.Properties | Foreach { $returnDetails.Properties[$_.Name] = $_.Value }
			}
		}
		return returnDetails
	}
	
	#########################################################################
	#	Get the resource groups broken into buckets.
	#########################################################################
	[GroupBuckets] GetGroupBuckets()
	{
		$returnBuckets = [GroupBuckets]::new()
		foreach($group in $this.ResourceGroups)
		{
			if($this.IsSpecialGroup($group.Name))
			{
				$returnBuckets.Special.Add($group.Name) > $null
			}
			elseif($group.Locks.Count -gt 0)
			{
				foreach($lockName in $group.Locks.Keys)
				{	
					if($group.Locks[$lockName] -like "CanNotDelete")
					{
						$returnBuckets.DeleteLocked.Add($group.Name) > $null
					}
					elseif($group.Locks[$lockName] -like "ReadOnly")
					{
						$returnBuckets.ReadOnlyLocked.Add($group.Name) > $null
					}
				}
			}
			else
			{
				$returnBuckets.Unlocked.Add($group.Name) > $null
			}
		}
		return $returnBuckets
	}
	
	#########################################################################
	#	Gets a summary of the current resource groups.
	#########################################################################
	[GroupSummary] GetGroupSummary(){
		$returnSummary = [GroupSummary]::new()
		
		foreach($rgroup in $this.ResourceGroups)
		{
			$returnSummary.TotalGroups += 1
			
			if($rgroup.Locks.Count -gt 0)
			{
				$returnSummary.LockedGroups += 1
			}
			
			if($rgroup.OlderThan60Days())
			{
				$returnSummary.OldGroups += 1
			}
			
			if($returnSummary.GroupDeployments.ContainsKey($rgroup.Location))
			{
				$returnSummary.GroupDeployments[$rgroup.Location] += 1
			}
			else
			{
				$returnSummary.GroupDeployments.Add($rgroup.Location,1)
			}
		}
		
		return $returnSummary
	}
	
	## PSUEDO PRIVATE
	
	
	#########################################################################
	#	Collect all resource groups into the internal $ResourceGroups param.
	#	This does NOT clear that list first, and is called from the constructor.
	#########################################################################
	hidden [void] CollectResourceGroups()
	{
		Write-Host("Collecting Resource Groups")

		$foundRgs = Get-AzureRmResourceGroup

		foreach($group in $foundRgs)
		{
			[ResourceGroup]$newGroup = [ResourceGroup]::new()
			$newGroup.Name = $group.ResourceGroupName
			$newGroup.Location = $group.Location
			$newGroup.Id = $group.ResourceId
			
			if($group.Tags)
			{
				foreach($key in $group.Tags.Keys)
				{
					$newGroup.Tags.Add($key, $group.Tags[$key])
				}
			}

			$locks = Get-AzureRmResourceLock -ResourceGroupName $group.ResourceGroupName
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
					$newGroup.Locks.Add($lock.Name, $lockType)
				}
			}
			
			$this.ResourceGroups.Add($newGroup) > $null
		}
	}
	
	#########################################################################
	#	Determines if a group name matches a default Azure RG name.
	#########################################################################
	hidden [bool] IsSpecialGroup([string]$groupName){
		$return = $false
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
				$groupName.Contains("databricks-") -or
				$groupName.Contains("fileserverrg-") -or
				($groupName -like 'MC*aks*')
				)
			)
		{
			$return = $true
		}
		
		return $return
	}
}