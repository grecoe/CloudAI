###############################################################
# Storage.ps1
#
#	Contains scripts to help with Azure Storage. At times its 
#	important to keep configuration data in a common place for 
#	many processes. 
#
#	The functions here are to help with that.
#
#
#	FUNCTIONS AVAILABLE
#			# Get a storage context object
#			GetStorageContext -acctname -acctkey
#			# Safely create a container if not there
#			CreateContainer -ctx -name

#			# Get deeper VM info for rg or whole sub
#			GetVirtualMachines [-subId] [-resourceGroup]
#			# Get VM summary for rg or whole sub
#			GetVirtualMachinesSummary [-subId] [-resourceGroup]
#			# Get VM status for specific instance
#			GetVmInformation [-subId] -resourceGroup -instanceName
#			# Stop a virtual machine, optionally deallocate
#			StopVirtualMachine [-subId] -resourceGroup -instanceName [flag]-deallocate
#			# Start a virtual machine
#			StartVirtualMachine [-subId] -resourceGroup -instanceName
#			# Summarize AML Compute Cluster Information
#			SummarizeComputeClusters -mlComputeInformation
#			# Find all AML Compute Clusters in subscription
#			FindMlComputeClusters [-subId]
#		
###############################################################

function CreateContext{
	Param([string]$acctName, [string]$accessKey)
	New-AzureStorageContext -StorageAccountName $acctName -StorageAccountKey $accessKey
}

function CreateContainer{
	Param($ctx, [string]$name)

	$created=$false
	$container=$null
	try
   	{
		$container = Get-AzureStorageContainer -Context $ctx -Name $name
		$created=$true
   	}
   	catch {
		$_.Exception.Message
   	}	

	if($container -eq $null)
	{
		$containerData = New-AzureStorageContainer -ErrorAction Stop -Context $ctx -Name $name -Permission Blob
		$created=$true
	}
	
	$created
}

function UploadLocalFile{
	Param($ctx, [string]$container, [string]$localPath, [string]$blobName)

	$uploadComplete=$false
	try
   	{
		$response = Set-AzureStorageBlobContent -Context $ctx -File $localPath -Container $container -Blob $blobName -Force
		$uploadComplete=$true
   	}
   	catch {
		$_.Exception.Message
   	}	
	
	$uploadComplete
}

function DownloadBlob{
	Param($ctx, [string]$container, [string]$localPath, [string]$blobName, [switch]$getText)

	$downloadComplete=$false
	try
   	{
		$result = Get-AzureStorageBlobContent -Force -Context $ctx -Container $container -Blob $blobName -Destination $localPath
		$downloadComplete=$true
   	}
   	catch {
		$_.Exception.Message
   	}	
	
	if($downloadComplete -and $getText)
	{
		$downloadComplete = Get-Content -ErrorAction Stop -Raw -Path $localPath
	}
	$downloadComplete
}


