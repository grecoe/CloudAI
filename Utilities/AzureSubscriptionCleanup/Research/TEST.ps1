

. './ResourceGroup.ps1'
. './Resources.ps1'
. './Compute.ps1'

#$subId = 'edf507a2-6235-46c5-b560-fd463ba2e771' #Danielle
$subId = '0ca618d2-22a8-413a-96d0-0f1b531129c3' #fang
#$subId = '03909a66-bef8-4d52-8e9a-a346604e0902' #Tao

$buckets = GetResourceGroupBuckets -subId $subId
$d2out = $buckets | ConvertTo-Json -depth 100
Write-Host($d2out)
break

$computeSummary = GetVirtualMachineComputeSummary -sub $subId
$groups = GetResourceGroupInfo 
$groupSummary = GetResourceGroupSummary -groups $groups
$resources = GetResources 

$subSummary = New-Object PSObject -Property @{ 
	GroupInfo=$groupSummary;
	Compute=$computeSummary;
	Resources=$resources}
$dout = $subSummary | ConvertTo-Json -depth 100
Write-Host($dout)
break

$expectedTags = ("alias", "project", "expires")

foreach ($g in $groups.GetEnumerator()) {

	$vmInformation = GetVirtualMachines -subId $subId -resourceGroup $g.Value.Name
	
	$totalVmInfo["Total"] += $vmInformation.Total
	$totalVmInfo["Running"] += $vmInformation.Running 
	$totalVmInfo["Deallocated"] += $vmInformation.Deallocated

	$rgInformation = New-Object PSObject -Property @{ 
			Name = $g.Value.Name; 
			Location=$g.Value.Location;
			Tags=(ParseTags -tags $g.Value.Tags);
			MissingTags=(FindMissingTags -tags $g.Value.Tags -expected $expectedTags);
			Locks=(ParseLocks -locks $g.Value.Locks);
			VirtualMachines=$vmInformation } 
			
	$output = $rgInformation | ConvertTo-Json -depth 100
	Write-Host($output)
	Write-Host("")
	Write-Host("")
}

$vmoutput = $totalVmInfo | ConvertTo-Json -depth 100

Write-Host($vmoutput)

Write-Host("Completed")
