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
#			# Upload a local file
#			UploadLocalFile -ctx -container -localPath -blobName
#			# Upload a local file
#			DownloadBlob -ctx -container -localPath -blobName [-getText]
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
		$container = Get-AzureStorageContainer -Context $ctx -Name $name -ErrorAction SilentlyContinue
		$created=$true
   	}
   	catch {
		Write-Host("Exception getting container")
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


