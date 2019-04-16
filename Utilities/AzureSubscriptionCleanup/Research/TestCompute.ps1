################################################################
#	Test getting compute resources.
################################################################

. './Azure/Subscription.ps1'
. './Azure/Compute.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

# Provide your subscription to verify
$subId = 'YOUR_SUBSCRIPTION_KEY' 

# Set context so we don't get hung up later
SetContext -subId $subId

#################################################################
# First get the ML Compute resources associated with AML Workspaces
#################################################################
$mlComputeClusters = FindMlComputeClusters
Write-Host("#############################################")
Write-Host("AML Workspace Compute Clusters")
Write-Host("#############################################")
$output = $mlComputeClusters | ConvertTo-Json
Write-Host($output)

#################################################################
# Summarize AML Compute Clusters
#################################################################
$mlComputeClustersSummary = SummarizeComputeClusters -mlComputeInformation $mlComputeClusters
Write-Host("#############################################")
Write-Host("AML Workspace Compute Clusters Summary")
Write-Host("#############################################")
$output = $mlComputeClustersSummary | ConvertTo-Json
Write-Host($output)

#################################################################
# Get the Virtual Machine Information
#################################################################
$vmCompute = GetVirtualMachines
Write-Host("#############################################")
Write-Host("Virtual Machine Information")
Write-Host("#############################################")
$output = $vmCompute | ConvertTo-Json
Write-Host($output)

#################################################################
# Get the GPU Virtual Machine Information
#################################################################
$vmComputeGPU = GetVirtualMachines -skuFilter '*nc*'
Write-Host("#############################################")
Write-Host("GPU Only Machine Information")
Write-Host("#############################################")
$output = $vmComputeGPU | ConvertTo-Json
Write-Host($output)

#################################################################
# Get the Virtual Machine Summary
#################################################################
$vmComputeSummary = GetVirtualMachinesSummary
Write-Host("#############################################")
Write-Host("Virtual Machine Summary")
Write-Host("#############################################")
$output = $vmComputeSummary | ConvertTo-Json
Write-Host($output)

#################################################################
# Summarize ALL VM Compute Resources
#################################################################
$allVmCompute = MergeComputeResources -mlClusterOverview $mlComputeClustersSummary -vmOverview $vmComputeSummary
Write-Host("#############################################")
Write-Host("Merged VM Information")
Write-Host("#############################################")
$output = $allVmCompute | ConvertTo-Json
Write-Host($output)

