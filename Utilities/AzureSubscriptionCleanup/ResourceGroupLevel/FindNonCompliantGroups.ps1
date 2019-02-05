#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
#
# Finds all groups that do not have the required tags
# applied to them (only if they are locked)
#####################################################
param(
	[string]$subId,
	[switch]$help=$false,
	[switch]$login=$false
)

#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host "This script will delete resource groups from an Azure Subscription."
	Write-Host "You will be prompted to log in but MUST provide a subscription ID."
	Write-Host "Parameters:"
	Write-Host "	-help : Shows this help message"
	Write-Host "	-subId : Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-login : Tells script to log into azure subscription, otherwise assumes logged in already"
	break
}


Set-StrictMode -Version 1

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

$resourceGroups = Get-AzureRmResourceGroup #| Where-Object {$_.ResourceGroupName -eq "dangtestdel"}

$expectedtags = New-Object System.Collections.ArrayList
$expectedtags.Add("alias") > $null
$expectedtags.Add("project") > $null
$expectedtags.Add("expires") > $null

$allTags ="alias,project,expires"

$totalGroups=0
$unlockedGroups=0
$compliantGroups=0
$nonCompliantGroups = @{}
$invalidDateGroups = @{}
$expiredGroups = @{}

#####################################################
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################
foreach($group in $resourceGroups)
{
	Write-Host("Validating : " + $group.ResourceGroupName)

	$totalGroups++

	$locks = Get-AzureRmResourceLock -ResourceGroupName $group.ResourceGroupName
	if($locks)
	{
		if($group.Tags)
		{
			$missingTags= New-Object System.Collections.ArrayList
			$groupTags = New-Object System.Collections.ArrayList

			# Get the tag names
			foreach($key in $group.Tags.Keys)
			{
				$groupTags.Add($key.Trim()) > $null
			}
			
			# Collect a list of missing tags
			foreach($expected in $expectedtags)
			{	
				if($groupTags.Contains($expected) -eq $false)
				{
					$missingTags.Add($expected) > $null
				}
			}
			
			# If missing tags, done and tag the group as non-compliant
			if($missingTags.Count -ne 0)
			{
				$missing = $missingTags -join ","
				$nonCompliantGroups.Add($group.ResourceGroupName, $missing)
			}
			else
			{
				# Have to validate the date
				$storedExpiredTag = $group.Tags["expires"]
				$parts = $storedExpiredTag -split '-'

				# If we don't get the three parts of YYYY-MM-DD then that's a problem.
				# If any of those parts are not numbers, also a problem.
				# Otherwise see if it's expired.
				if($parts.Length -ne 3)
				{
					$invalidDateGroups.Add($group.ResourceGroupName, $storedExpiredTag)
				}
				else
				{
					$validInput = $true
					foreach($part in $parts)
					{
						if( ($part.Trim() -match '^[0-9]+$') -eq $false)
						{
							$validInput = $false
						}
					}
					
					# validInput tells us if its only got numbers
					if($validInput)
					{
						$year = [convert]::ToInt32($parts[0].Trim(), 10) 
						$month = [convert]::ToInt32($parts[1].Trim(), 10)
						$day = [convert]::ToInt32($parts[2].Trim(), 10) 
					
						$today = Get-Date
						$expirationDate = Get-Date -Year $year -Month $month -Day $day
					
						# If it's not expired, it's compliant, if not it's expired.
						if($today -gt $expirationDate)
						{
							# The group has expired
							$expiredGroups.Add($group.ResourceGroupName, $storedExpiredTag)
						}
						else
						{
							# The group is compliant and not expired.
							$compliantGroups++
						}
					}
					else
					{
						# This group has a bad date format stored.
						$invalidDateGroups.Add($group.ResourceGroupName, $storedExpiredTag)
					}
				}
			}
			
		}
		else
		{
			$nonCompliantGroups.Add($group.ResourceGroupName, $allTags) 
		}
	}
	else
	{
		$unlockedGroups++
	}
}

#Create a directory
md -ErrorAction Ignore -Name $subId

$outofcompliance = New-Object PSObject -Property @{ 
	Subscription = $subId; 
	Total=$totalGroups; 
	Unlocked = $unlockedGroups;
	Compliant = $compliantGroups;
	NonCompliant = $nonCompliantGroups;
	InvalidDate = $invalidDateGroups;
	Expired = $expiredGroups}
	
Out-File -FilePath .\$subId\resource_group_compliance.json -InputObject ($outofcompliance | ConvertTo-Json -depth 100)

