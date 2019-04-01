

. './RgUtils.ps1'
. './ResourceUtils.ps1'
. './MlCompute.ps1'

$subId = '8dd18bd7-b9dc-4213-b367-cd6a7745bc88'

$expectedTags = ("alias", "project", "expires")
#$groups = Get-ResourceGroupInfo -sub '0ca618d2-22a8-413a-96d0-0f1b531129c3'
#$groups = Get-ResourceGroupInfo -sub $subId

$totalVmInfo=@{}
$totalVmInfo.Add("Total", 0)
$totalVmInfo.Add("Running", 0)
$totalVmInfo.Add("Deallocated",0)

$wspaces = FindMlComputeClusters -sub $subId
$dout = $wspaces | ConvertTo-Json -depth 100
Write-Host($dout)
break

foreach ($g in $groups.GetEnumerator()) {

	$vmInformation = GetVirtualMachines -sub $subId -resourceGroup $g.Value.Name
	
	$totalVmInfo["Total"] += $vmInformation.Total
	$totalVmInfo["Running"] += $vmInformation.Running 
	$totalVmInfo["Deallocated"] += $vmInformation.Deallocated

	$rgInformation = New-Object PSObject -Property @{ 
			Name = $g.Value.Name; 
			Location=$g.Value.Location;
			Tags=(ParseTags -tags $g.Value.Tags);
			MissingTags=(FindMissingTags -tags $g.Value.Tags -expected $expectedTags);
			Locks=(ParseLocks -locks $g.Value.Locks);
			VirtualMachines=$vmInformation } #(GetVirtualMachines -resourceGroup $g.Value.Name)}
			
	$output = $rgInformation | ConvertTo-Json -depth 100
	Write-Host($output)
	Write-Host("")
	Write-Host("")
}

$vmoutput = $totalVmInfo | ConvertTo-Json -depth 100

Write-Host($vmoutput)

Write-Host("Completed")
