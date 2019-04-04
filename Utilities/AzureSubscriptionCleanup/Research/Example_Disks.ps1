################################################################
#	Quite a few VHD or AzureDisk objects can be left behind in a
#	series of changes over time to machines, or whatever. These 
#	examples show you how to find and delete these unwanted cruft
#	from the subscription.
################################################################

# You may need to update your Azure CLI to do this. 
# https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest

. './Azure/Subscription.ps1'
. './Azure/Resources.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

# Provide your subscription to verify
$subId = 'YOUR_SUBSCRIPTION_ID' 

# Set context so we don't get hung up later
SetContext -subId $subId

$disks = FindUnmanagedDisks -unlockedOnly
Write-Host(($disks | ConvertTo-Json))
foreach($disk in $disks.GetEnumerator())
{
	# Careful, if you don't really want to delete, don't execute this.
	DeleteAzureDisk -resourceGroup $disk.Value.ResourceGroup -diskName $disk.Value.DiskName
}

#$vhdBlobs = FindUnleasedVhdFiles 
#Write-Host(($vhdBlobs | ConvertTo-Json))
