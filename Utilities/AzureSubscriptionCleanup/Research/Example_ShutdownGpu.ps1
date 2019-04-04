##########################################################################
#	Example on how to shut down ONLY GPU VM in Subscription
##########################################################################

. './Azure/Subscription.ps1'
. './Azure/Compute.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

# Provide your subscription to verify
$subId = '0ca618d2-22a8-413a-96d0-0f1b531129c3' 

# Set context so we don't get hung up later
Write-Host("Setting context to sub: " + $subId)
SetContext -subId $subId

# Get GPU VM
Write-Host("Collect GPU VM List")
$vmComputeGPU = GetVirtualMachines -skuFilter '*nc*'

Write-Host("Shutdown GPU VMs")
foreach($gpu in $vmComputeGPU)
{
	# Running state would be checked inside of StopVirtual machine, just showing how to do it.
	if($gpu.Running)
	{
		StopVirtualMachine -resourceGroup $gpu.Name -instanceName $gpu.Instance -$deallocate
	}
}
