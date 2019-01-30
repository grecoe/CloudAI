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
$expectedtags.Add("lifespan") > $null

$outofcompliancegroups = @{}

#####################################################
# Go through each one looking at locks. Add them to
# the appropriate list - locked unlocked.
#####################################################
foreach($group in $resourceGroups)
{
	Write-Host("Validating : " + $group.ResourceGroupName)
	$locks = Get-AzureRmResourceLock -ResourceGroupName $group.ResourceGroupName
	if($locks)
	{
		if($group.Tags)
		{
			$foundtags = New-Object System.Collections.ArrayList
			foreach($key in $group.Tags.Keys)
			{
				$foundtags.Add($key) > $null
			}
			
			foreach($expected in $expectedtags)
			{	
				if($foundtags.Contains($expected) -eq $false)
				{
					if($outofcompliancegroups.ContainsKey($group.ResourceGroupName) -eq $false)
					{
						$outofcompliancegroups.Add($group.ResourceGroupName, (New-Object System.Collections.ArrayList))
					}
					
					$outofcompliancegroups[$group.ResourceGroupName].Add($expected) > $null
				}
			}
		}
		else
		{
			$outofcompliancegroups.Add($group.ResourceGroupName,(New-Object System.Collections.ArrayList))
			$outofcompliancegroups[$group.ResourceGroupName].Add("No tags") > $null
		}
	}
}

#Create a directory
md -ErrorAction Ignore -Name $subId

$outofcompliance = New-Object PSObject -Property @{ 
	Total = $outofcompliancegroups.Count; 
	NonCompliantGroups = $outofcompliancegroups}
	
Out-File -FilePath .\$subId\non_compliant_groups.json -InputObject ($outofcompliance | ConvertTo-Json -depth 100)

