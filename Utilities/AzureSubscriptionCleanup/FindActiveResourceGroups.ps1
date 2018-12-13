#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
# whatif - Run it as if we would delete but only display
#		   what would happen if executed.
#####################################################
param(
	[string]$subId,
	[int]$hours=2,
	[switch]$help=$false
)

#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host ""
	Write-Host "This script will collect log activity for each Azure Resource Group from the "
	Write-Host "provided Azure Subscription. Output will be written to a file called"
	Write-Host "SUBSCRIPITON_ID.json"
	Write-Host ""
	Write-Host "You will be prompted to log in but MUST provide a subscription ID."
	Write-Host ""
	Write-Host "Parameters:"
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-hours : Integer number of hours in the past to search."
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
Write-Host "Log into Azure...."
#Login-AzureRmAccount
Write-Host "Setting subscription ID : $subId"
Set-AzureRmContext -SubscriptionID $subId

#####################################################
# Collect all of the resource groups
#####################################################
Write-Host "Scanning subscription for resource groups, this could take a while....."

$resourceGroups = Get-AzureRmResourceGroup #| Where-Object {$_.ResourceGroupName -eq "dangtestdel"}

#####################################################
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################

#Setup the time information
$hours = $hours * -1
$timeWindow = [System.DateTime]::Now.AddHours($hours)
$timeWindowFormat = $timeWindow.ToString("yyyy-MM-ddThh:mm")
Write-Host("Searching " + $hours + " in the past to timesstamp " + $timeWindowFormat)

#Collections for data read
$allGroupsWithActivities = New-Object System.Collections.ArrayList
$allGroupWithoutActivities = New-Object System.Collections.ArrayList
foreach($group in $resourceGroups)
{
	Write-Host("Scanning group " + $group.ResourceGroupName)
	$azureLog = Get-AzureRmLog -WarningAction silentlyContinue -ResourceGroup $group.ResourceGroupName -StartTime $timeWindowFormat | Where-Object {$_.Authorization.Action -ne "Microsoft.Authorization/CheckAccess/action"}
	$activityInfo = @{}
	foreach($logEvent in $azureLog)
	{
		if(![System.String]::IsNullOrEmpty($logEvent.Authorization.Action))
		{
			if($activityInfo.ContainsKey($logEvent.Authorization.Action) -eq $false)
			{
				$activityInfo.Add($logEvent.Authorization.Action,1) > $null
			}	
			else
			{
				$activityInfo[$logEvent.Authorization.Action]++
			}
		}
	}
	
	if($activityInfo.Count -ne 0)
	{
		$rgActivities = New-Object PSObject -Property @{ Name = $group.ResourceGroupName; Events = $activityInfo }
		$allGroupsWithActivities.Add($rgActivities) > $null
	}
	else
	{
		$allGroupWithoutActivities.Add($group.ResourceGroupName) > $null
	}
}

$overview = New-Object PSObject -Property @{ 
		Subscription = $subId; 
		TimeWindow = $timeWindowFormat; 
		Active = $allGroupsWithActivities; 
		Inactive = $allGroupWithoutActivities 
	}


$outputOverview = $overview | ConvertTo-Json -depth 100
$fileName = $subId + "activitylogs.json"
Out-File -FilePath .\$fileName -InputObject $outputOverview
Write-Host("Completed")