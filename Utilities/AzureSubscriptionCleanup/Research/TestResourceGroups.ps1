#######################################################################
#	Example on how to get general information about all resource groups
#	in a specified subscripiton.
#######################################################################

. './Azure/ResourceGroup.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

# Provide your subscription to verify
$subId = 'YOUR_SUBSCRIPTION_ID' 

# Set context so we don't get hung up later
SetContext -subId $subId

# Get the resource group information
$groups = GetResourceGroupInfo 

$results = @{}
foreach ($group in $groups.GetEnumerator()) {

	$tags = (ParseTags -tags $group.Value.Tags).Keys -join ','
	$locks = (ParseLocks -locks $group.Value.Locks).Keys -join ','
	$rgInformation = New-Object PSObject -Property @{ 
			Location=$group.Value.Location;
			Tags=$tags;
			Locks=$locks} 
			
	$results.Add($group.Value.Name, $rgInformation)
}

Write-Host("#############################################")
Write-Host("General Group Information " + $subId)
Write-Host("#############################################")
$output = $results | ConvertTo-Json
Write-Host($output)

$groupSummary = GetResourceGroupSummary -groups $groups
Write-Host("#############################################")
Write-Host("Group Summary Information (no history) " + $subId)
Write-Host("#############################################")
$output = $groupSummary | ConvertTo-Json
Write-Host($output)
