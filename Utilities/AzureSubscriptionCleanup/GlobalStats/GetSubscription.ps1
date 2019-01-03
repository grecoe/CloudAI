#####################################################
# It is expected that you are already logged in to 
# Azure before running this, if not run:
#	Login-AzureRMAccount
#
# Collect all of the subscription information and 
# write it out to a local file. 
#####################################################
$subscriptions = Get-AzureRmSubscription
$subscriptionList = New-Object System.Collections.ArrayList
foreach( $sub in $subscriptions)
{
	$subDetail = New-Object PSObject -Property @{ Name = $sub.Name; Id = $sub.Id; State = $sub.State }
	$subscriptionList.Add($subDetail) > $null
}
Out-File -FilePath .\subscriptionlist.json -InputObject ($subscriptionList | ConvertTo-Json -depth 100)

#####################################################
# For each subscripiton run the VM and RG scripts
#####################################################
$resconstitutedSubs = Get-Content -Raw -Path .\subscriptionlist.json | ConvertFrom-Json

# Collect VM and resource group information
foreach($rsub in $resconstitutedSubs)
{
	Write-Host($rsub.Name)
	.\GetVMInfoAndConfig.ps1 -subId $rsub.Id
	.\DelresourceGroups.ps1 -whatif -subId $rsub.Id
}

#####################################################
# Roll up the stats for all subscriptions.
#####################################################
.\CollectSubStats.ps1