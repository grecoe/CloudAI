#####################################################
# Parameters for the script
# subId - Required - need a subscription ID to work on
# help - Switch to show usage
# whatif - Run it as if we would delete but only display
#		   what would happen if executed.
#####################################################
param(
	[string]$subId,
	[string]$resourceFile,
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
	Write-Host "This script finds deployments of resources listed in the input file. That file has the form:"
	Write-Host "{"
	Write-Host "	'resources' : ["
	Write-Host "		'resourcetypename',"
	Write-Host "		'...',"
	Write-Host "		]"
	Write-Host "}"
	Write-Host ""
	Write-Host "Output file in directory subid\resourcedeployment.json"
	Write-Host ""
	Write-Host "Parameters:"
	Write-Host "	-subId [subid]: Required on all calls EXCEPT help. Identifies the subscription to scrub."
	Write-Host "	-resourceFile [file]: Required on all calls EXCEPT help. The file path containing resources to find" 
	Write-Host "	-login : Tells script to log into azure subscription, otherwise assumes logged in already"
	Write-Host "	-help : Optional, presence means to show this help message"
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

if(-not $resourceFile)
{
	Write-Host "-resourceFile is a required parameter. Example: Microsoft.CognitiveServices/accounts"
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

Write-Host ("Setting subscription ID : " + $subId )
Set-AzureRmContext -SubscriptionID $subId



#####################################################
# Collect all of the resource groups
#####################################################

#load resources
Write-Host("Loading content from " + $resourceFile)
$resourceList = Get-Content -Raw -Path $resourceFile | ConvertFrom-Json

Write-Host($resourceList.resources.Contains("Microsoft.MachineLearningModelManagement/accounts"))

# Create a dictionary of type
# ResourceGroup:
#	Resource:Count
$resourceListDictionary = @{}

foreach($resType in $resourceList.resources)
{
	Write-Host("Searching for resources of type : " + $resType)
	$resources = Get-AzureRmResource -ResourceType $resType
	foreach($res in $resources)
	{
		if($resourceListDictionary.ContainsKey($res.ResourceGroupName) -eq $false)
		{
			$resourceListDictionary.Add($res.ResourceGroupName,@{})
		}
		
		if($resourceListDictionary[$res.ResourceGroupName].ContainsKey($resType) -eq $false)
		{
			$resourceListDictionary[$res.ResourceGroupName].Add($resType,0)
		}
		$resourceListDictionary[$res.ResourceGroupName][$resType]++
	}
}

Write-Host ""

#Create a directory
md -ErrorAction Ignore -Name $subId
	
# Output the data to a file.....
$overview = New-Object PSObject -Property @{ 
		Subscription = $subId; 
		Instances = $resourceListDictionary; 
	}
	
$fileName = "resourcedeployment.json"
Out-File -FilePath .\$subId\$fileName -InputObject ($overview | ConvertTo-Json -depth 100)

Write-Host("Completed")