
class BlobStorage {
	[string]$StorageAccount
	[string]$StorageKey
	$StorageContext
	
	BlobStorage([string]$name, [string]$key) {
		$this.StorageAccount = $name
		$this.StorageKey = $key
		$this.StorageContext = New-AzureStorageContext -StorageAccountName $this.StorageAccount -StorageAccountKey $this.StorageKey
	}

	[bool] CreateContainer([string]$name) {
		$created=$false
		$container=$null
		try
		{
			$container = Get-AzureStorageContainer -Context $this.StorageContext -Name $name -ErrorAction SilentlyContinue
			$created=$true
		}
		catch {
			Write-Host("Exception getting container")
			$_.Exception.Message
		}	
	
		if($container -eq $null)
		{
			$containerData = New-AzureStorageContainer -ErrorAction Stop -Context $this.StorageContext -Name $name -Permission Blob
			$created=$true
		}
		
		return $created
	}
	
	[bool] UploadFile([string]$container, [string]$localPath, [string]$blobName){
		$uploadComplete=$false
		try
		{
			$response = Set-AzureStorageBlobContent -Context $this.StorageContext -File $localPath -Container $container -Blob $blobName -Force
			$uploadComplete=$true
		}
		catch {
			$_.Exception.Message
		}	
		
		return $uploadComplete
	}

	[bool] DownloadBlob([string]$container, [string]$localPath, [string]$blobName){
		$downloadComplete=$false
		try
		{
			$result = Get-AzureStorageBlobContent -Force -Context $this.StorageContext -Container $container -Blob $blobName -Destination $localPath
			$downloadComplete=$true
		}
		catch {
			$_.Exception.Message
		}	
		
		return $downloadComplete
	}

	[string] DownloadBlobContent([string]$container, [string]$localPath, [string]$blobName){
		$downloadContent=$null
		try
		{
			$result = Get-AzureStorageBlobContent -Force -Context $this.StorageContext -Container $container -Blob $blobName -Destination $localPath
			$downloadContent = Get-Content -ErrorAction Stop -Raw -Path $localPath
		}
		catch {
			$_.Exception.Message
		}	
		
		return $downloadContent
	}
}




