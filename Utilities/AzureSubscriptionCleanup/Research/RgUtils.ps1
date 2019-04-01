###############################################################
# Get-ResourceGroupInfo
#
#	Get a list of all resource groups in a subscription with 
#	some supporting information
#
#	Params:
#		sub : Subscription ID
#
#	Returns:
#		Hashtable<[string]subname, [object]info>
#			info = 
#			Name - String
#			Location - String  
#			Locks - PSObject 
#			Tags - PSObject
###############################################################
function Get-ResourceGroupInfo {
	Param ([string]$sub)

	$returnTable = @{}
	
	Write-Host("Logging in to sub " + $sub)
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