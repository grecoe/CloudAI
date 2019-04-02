

###############################################################
# GetResourceGroupSummary
#
#	Collects a summary of resource groups in the subscription.
#
#	Params:
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
	Param ($groups, [switch]$history)

	#$context = Set-AzureRmContext -SubscriptionID $subId

	#$groups = GetResourceGroupInfo -sub $subId
	
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
		if($g.Value.Name.Contains("cleanup") -or
		   $g.Value.Name.Contains("Default-Storage-") -or
		   ( $g.Value.Name.Contains("DefaultResourceGroup-") -or
			 $g.Value.Name.Contains("Default-MachineLearning-") -or
			 $g.Value.Name.Contains("cloud-shell-storage-") -or
			 $g.Value.Name.Contains("Default-ServiceBus-") -or
			 $g.Value.Name.Contains("Default-Web-") -or
			 $g.Value.Name.Contains("OI-Default-") -or
			 $g.Value.Name.Contains("Default-SQL") -or
			 $g.Value.Name.Contains("StreamAnalytics-Default-") -or
			 $g.Value.Name.Contains("databricks-")
			)
		  )
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
#		groupName : Resource Group Name
#
#	Returns:
#		Boolean flag, $true is >60 days
###############################################################
function ResourceGroupOlderThan60Days {
	Param ([string]$groupName)
	
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
#		sub : Subscription ID
#
#	Returns:
#		Hashtable<[string]subname, [object]info>
#			Name - String
#			Location - String  
#			Locks - PSObject 
#			Tags - PSObject
###############################################################
function GetResourceGroupInfo {
	Param ([string]$sub)

	$returnTable = @{}
	
	$context = Set-AzureRmContext -SubscriptionID $sub

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
#	
#
#	Params:
#		tags : Tags hash table associated with a resource group
#		expected : Tag names expected to be found
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