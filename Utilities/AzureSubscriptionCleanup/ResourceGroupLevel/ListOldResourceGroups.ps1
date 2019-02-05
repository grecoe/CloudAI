#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
# whatif - Run it as if we would delete but only display
#		   what would happen if executed.
#####################################################
param(
	[string]$subId,
	[switch]$login=$false,
	[switch]$help=$false
)

#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host ""
	Write-Host "This script will collect all resource groups that have no activity for the last 60-90"
	Write-Host "days indicating they are new."
	Write-Host ""
	Write-Host "You will be prompted to log in but MUST provide a subscription ID."
	Write-Host ""
	Write-Host "Parameters:"
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-login : Tells script to log into azure subscription, otherwise assumes logged in already"
	Write-Host "	-help : Shows this help message"
	break
}

#####################################################
# Verify that subId is actually provided
#####################################################
if(-not $subId)
{
	Write-Host "-subId is a required parameter. Run the script with -help to get more information."
	break
}


#####################################################
# Log in and set to the sub you want to see
#####################################################
if($login -eq $true)
{
	Write-Host "Log into Azure...."
	Login-AzureRmAccount
}
else
{
	Write-Host "Bypassing Azure Login...."
}

Write-Host "Setting subscription ID : $subId"
Set-AzureRmContext -SubscriptionID $subId



#####################################################
# Collect all of the resource groups
#####################################################
Write-Host "Collecting Resource Groups"
$resourceGroups = Get-AzureRmResourceGroup #| Where-Object {$_.ResourceGroupName -eq "dangtestvm"}

$rgTotalList = New-Object System.Collections.ArrayList
$rgOldList = New-Object System.Collections.ArrayList

Write-Host "Iterating over groups....."
foreach($group in $resourceGroups)
{
	Write-Host($group.ResourceGroupName)

	$rgTotalList.Add($group.ResourceGroupName) > $null
	
	# Find-AzureRmResource deprecated in breaking change....
	#$resources = Find-AzureRmResource -ResourceGroupNameEquals $group.ResourceGroupName
	$resources = Get-AzureRmResource -ResourceGroupName $group.ResourceGroupName
	
	$pointInTime = [DateTime]::Now.AddDays(-60)
	$horizon = $pointInTime.AddDays(-29)

	foreach($res in $resources)
	{
		#Write-Host("Group: " + $res.ResourceGroupName + " Name: " + $res.Name)
	
		$logs = Get-AzureRmLog -StartTime $horizon -EndTime $pointInTime -Status "Succeeded" -ResourceId $res.ResourceId -WarningAction "SilentlyContinue" `
		
		if($logs.Count -gt 0)
		{
			$rgOldList.Add($group.ResourceGroupName) > $null
			break
		}
	}
}	


# Remove older groups from all groups
foreach($rgname in $rgOldList)
{
	if($rgTotalList.Contains($rgName))
	{
		$rgTotalList.Remove($rgName)
	}
}


Write-Host("")
Write-Host("")

Write-Host("---------------------------")
Write-Host("Groups older than 60 days")
Write-Host("---------------------------")
foreach($rgname in $rgOldList)
{
	Write-Host($rgname)
}

Write-Host("---------------------------")
Write-Host("New Groups")
Write-Host("---------------------------")
foreach($rgname in $rgTotalList)
{
	Write-Host($rgname)
}

#Create a directory
md -ErrorAction Ignore -Name $subId

$outputOverview = $rgOldList | ConvertTo-Json -depth 100
$fileName = "old_resource_groups.json"
Out-File -FilePath .\$subId\$fileName -InputObject $outputOverview

Write-Host("Completed")






