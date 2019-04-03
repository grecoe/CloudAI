##########################################################################
#	Compliance means different things to different groups. As a 
#	developer, you can use the tools to determine group compliance
#	based on rules you have set.
#	
#	In this example, compliance means a group meets these requirements
#		- A Delete or ReadOnly lock has been applied to the resource
#		group OR one if it's descendants.
#		- A set of tags are present.
##########################################################################

. './Azure/Subscription.ps1'
. './Azure/ResourceGroup.ps1'

###############################################################
# GetComplianceInformation
#
#	Using the rules above, figure out what is compliant or not.
#
#	Params:
#		subId : Subscription to work on.
#
#	Returns: PSObject
#		Compliant - List of RG names that are compliant
#		Ignored - List of RG names that are Azure default groups.
#		NonCompliant<[string]GroupName, [List]describes what is missing>
###############################################################
function GetComplianceInformation {
	Param([string]$subId)
	
	Write-Host("Compliance Information")
	SetContext -subId $subId
	
	# What tags are required to meet compliance? Make up your own.
	$expectedTags = ("alias", "project", "expires")
	
	# Get the resource groups
	$groups = GetResourceGroupInfo
	
	# How will it be reported. In this case, we want the names of the 
	# compliant groups. For special (Azure Defaults) we just want to know 
	# what they are, but we won't look further. For non-compliant groups, 
	# we will include an array containing what is missing, i.e. Lock, Tag
	$comliantGroups = New-Object System.Collections.ArrayList
	$nonVerified = New-Object System.Collections.ArrayList
	$nonComliantGroups = @{}
	
	# Go through the groups
	foreach($group in $groups.GetEnumerator())
	{
		# Check Special First, if so, ignore it.
		$special = IsSpecialGroup -groupName $group.Value.Name
		if($special -eq $true)
		{
			$nonVerified.Add($group.Value.Name) > $null
			continue
		}
		
		# If there is no locks OR no tags, it's automatically not compliant
		$foundTags = ParseTags -tags $group.Value.Tags
		$foundLocks = ParseLocks -locks $group.Value.Locks
		
		if( ($foundTags.Count -eq 0) -or ($foundLocks.Count -eq 0))
		{
			$complianceIssues = New-Object System.Collections.ArrayList
			if($foundTags.Count -eq 0)
			{
				$complianceIssues.Add("Tags") > $null
			}
			if($foundLocks.Count -eq 0)
			{
				$complianceIssues.Add("Locks") > $null
			}
			$nonComliantGroups.Add($group.Value.Name, $complianceIssues)
			continue
		}
		
		# If we are here, it's not special, a lock exists and some tags exist. 
		$missingTags = FindMissingTags -tags $group.Value.Tags -expected $expectedTags
		if($missingTags.Count -gt 0)
		{
			$complianceIssues = New-Object System.Collections.ArrayList
			$complianceIssues.Add("Tags") > $null
			$nonComliantGroups.Add($group.Value.Name, $complianceIssues)
			continue
		}
		
		# If we are here, the group is compliant
		$comliantGroups.Add($group.Value.Name) > $null
	}
	
	# build return object
	$complianceOverview = New-Object PSObject -Property @{ 
		Compliant=$comliantGroups;
		Ignored=$nonVerified;
		NonCompliant=$nonComliantGroups}
		
	$complianceOverview
}
