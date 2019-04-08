########################################################################
#	Examples on how to use SubReview. This collects information about
#	a specific subscription and is driven from the SubReview.JSON file.
#	
#	Field			Type	Description
#	compute			bool	Include compute summary in output.
#	countResources	bool	Include global resource counts 
#	deployments		array	Name of resource types to find
#	compliance		object	Compliance information to find.
#							lockState - String- all, locked, unlocked
#										Lock restrictions to check
#							tags - object - List of tags to check
#								Name - String - Name of tags
#								Verify - bool - Check value exists
#	results			object	Should results be dumped to a file?
#							localPath - String - if set results to 
#										a local file. Otherwise goes
#										to storage defined.
#							storageAccount - string
#							storageKey - string 
#							container - string
#							blob - string
########################################################################

. './Azure/Subscription.ps1'
. './Azure/Compute.ps1'
. './Azure/Resources.ps1'
. './Azure/Storage.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

# Provide your subscription to verify
$subId = 'YOUR_SUBSCRIPTION_KEY' 
$configurationFile = '.\SubReview.json'

# Set context so we don't get hung up later
Write-Host("Setting context to subscription : " + $subId)
SetContext -subId $subId

# Load configuration
$configuration = Get-Content -ErrorAction Stop -Raw -Path $configurationFile | ConvertFrom-Json

########################################################################
# If configuration.compute is true, get a run down of the compute 
# resources contained in the subscription
########################################################################
function GetComputeResources{
	$mlComputeClusters = FindMlComputeClusters
	$mlComputeClustersSummary = SummarizeComputeClusters -mlComputeInformation $mlComputeClusters
	$vmComputeSummary = GetVirtualMachinesSummary
	
	MergeComputeResources -mlClusterOverview $mlComputeClustersSummary -vmOverview $vmComputeSummary
}

########################################################################
# If configuration.deployments.Count is greater than 0, get the deploy
# information for the listed resource types.
########################################################################
function GetDeployments {
	Param($deploymentList)
	
	$returnValue = @{}
	foreach($resType in $deploymentList)
	{
		$deploy = FindDeployments -resourceType $resType
		
		$returnValue.Add($resType, $deploy)
	}
	
	$returnValue
}

########################################################################
# If configuration.tagCompliance.tags.Count is greater than 0, get the 
# compliance data with regards to tags. 
########################################################################
function VerifyCompliance {
	Param([string]$subId, $complianceInfo)

	$complianceResults = @{}
	
	$unlockedKey="Unlocked"
	$dLockedKey="DeleteLocked"
	$roLockedKey="ReadOnlyLocked"
	$targetGroups = New-Object System.Collections.ArrayList

	# Collect the resource group buckets
	$resourceGroups = GetResourceGroupBuckets
	
	# Figure out what buckets to look at. 
	$lockTargets = New-Object System.Collections.ArrayList
	if($complianceInfo.lockState -eq "all")
	{
		$lockTargets.Add($unlockedKey) > $null
		$lockTargets.Add($dLockedKey) > $null
		$lockTargets.Add($roLockedKey) > $null
	}
	elseif($complianceInfo.lockState -eq "locked")
	{
		$lockTargets.Add($dLockedKey) > $null
		$lockTargets.Add($roLockedKey) > $null
	}
	else
	{
		$lockTargets.Add($unlockedKey) > $null
	}
	
	# Make sure that we got at least one bucket. Can be empty  
	# if config.json isn't "all", "locked" or "unlocked"
	if($lockTargets.Count -gt 0)
	{
		$expectedTags = New-Object System.Collections.ArrayList
		foreach($ftag in $complianceInfo.tags)
		{
			$expectedTags.Add($ftag.Name) > $null
		}
		
		# For each bucket, collect the groups to look at. 
		foreach($bucket in $lockTargets)
		{
			if($resourceGroups.ContainsKey($bucket))
			{
				foreach($targetGroup in $resourceGroups[$bucket])
				{
					$targetGroups.Add($targetGroup) > $null
				}
			}
		}
		
		# Work with each group
		Write-Host("Target groups")
		Write-Host (($targetGroups | ConvertTo-Json))
		foreach($target in $targetGroups)
		{
			Write-Host("Load group details: " + $target)
			$groupDetails = LoadDetailedResourceGroup -resourceGroup $target
		
			$groupTags = ParseTags -tags $groupDetails.Tags
			$missingTags = FindMissingTags -tags $groupTags -expected $expectedTags
			$invalidTagContent = New-Object System.Collections.ArrayList
	
			# Verify values
			foreach($ctag in $complianceInfo.tags)
			{
				if($ctag.verify -and $groupTags.ContainsKey($ctag.Name))
				{
					# If missing it would be in the missingTags list
					if([string]::IsNullOrEmpty($groupTags[$ctag.Name]))
					{
						$invalidTagContent.Add($ctag.Name) > $null
					}
				}
			}

			# Collect the return information.
			if(($invalidTagContent.Count -eq 0) -and ($missingTags.Count -eq 0))
			{
				$complianceResults.Add($target, "OK")
			}
			else
			{
				$nonCompliance=@{}
				if($invalidTagContent.Count -gt 0)
				{
					$nonCompliance.Add("Invalid Content", $invalidTagContent)
				}
				
				if($missingTags.Count -gt 0)
				{
					$nonCompliance.Add("Missing Tags", $missingTags)
				}

				$complianceResults.Add($target, $nonCompliance)
			}
		}
	}

	Write-Host(($complianceResults | ConvertTo-Json))
	$complianceResults
}


$computeResources=$null
$allResources=$null
$deployments=$null
$complianceInfo=$null


########################################################################
# If config asks for compute, collect it.
########################################################################
if($configuration.compute)
{
	Write-Host "Collecting compute resources..." -ForegroundColor White
	$computeResources = GetComputeResources
}

########################################################################
# If config asks for resource counts, collect it.
########################################################################
if($configuration.countResources)
{
	Write-Host "Collecting general resource counts..."  -ForegroundColor White
	$allResources = GetResources
}

########################################################################
# If config asks for deployment information, collect it.
########################################################################
if($configuration.deployments.Count -gt 0)
{
	Write-Host "Collecting specific resource deployments..."  -ForegroundColor White
	$deployments = GetDeployments -deploymentList $configuration.deployments
}

########################################################################
# If config asks for tag compliance information, collect it.
########################################################################
if($configuration.tagCompliance.tags.Count -gt 0)
{
	Write-Host "Verify Compliance Information...."  -ForegroundColor White
	$complianceInfo = VerifyCompliance -complianceInfo $configuration.tagCompliance
}


########################################################################
# Get the output
########################################################################
$outputInfo = New-Object PSObject -Property @{ 
		ComputeResources = $computeResources;
		AllResources= $allResources;
		Deployments=$deployments;
		Compliance=$complianceInfo}
$outputData = $outputInfo | ConvertTo-Json -depth 100

########################################################################
# If a local path is provided, dump it here.
########################################################################
if($configuration.results.localPath)
{
	Write-Host "Dump results to : "  $configuration.results.localPath
	Out-File -FilePath $configuration.results.localPath -InputObject $outputData
}

########################################################################
# If Azure Storage info is provided, upload it.
########################################################################
if(	$configuration.results.storageAcct -and 
	$configuration.results.storageKey -and 
	$configuration.results.container -and 
	$configuration.results.blob)
{
	$localStorageFile = '.\' + (Get-Date -UFormat "%Y-%m-%d") + $configuration.results.blob
	$storageDatedContainerName = (Get-Date -UFormat "%Y-%m-%d") + $configuration.results.container
	$ctx = CreateContext -acctName $configuration.results.storageAcct -accessKey $configuration.results.storageKey
	$createResult = CreateContainer -ctx $ctx -name $storageDatedContainerName
	
	Out-File -FilePath $localStorageFile -InputObject $outputData
	$uploadResult = UploadLocalFile -ctx $ctx -container $storageDatedContainerName -localPath $localStorageFile -blobName $configuration.results.blob
	
	if (Test-Path $localStorageFile) 
	{
		Remove-Item $localStorageFile
	}	
}
