# Perform a login prior to calling this.

Using module .\clsBlobStorage.psm1
Using module .\clsSubscription.psm1

# Information we'll need along the way
$account='YOUR_STORAGE_ACCOUNT'
$key='YOUR_STORAGE_ACCOUNT_KEY'

$containerName = 'classtest'
$localFile ='.\testfile.txt'
$blobName='foo.json'
$downloadLocalFile ='.\testfile2.txt'

# Generate some information and write it to a file
$randomData = @{}
$randomData.Add("first","data")
Out-File -FilePath $localFile -InputObject ($randomData | ConvertTo-Json -depth 100)

# Set up subscription manager
$subManager = [SubscriptionManager]::new()
$currentSubscription = $null

# Filter on subscriptions 
$subscriptionNameToFind="Danielle"
Write-Host("Searching for:  " + $subscriptionNameToFind )
$result = $subManager.FindSubscription($subscriptionNameToFind)

if($result.Count -eq 1)
{
	$currentSubscription = $result[0]
	Write-Host("Working with subscription " + $currentSubscription.Name)
	$subManager.SetSubscription($currentSubscription)

	$blobUtility = [BlobStorage]::new($account, $key)
	$blobUtility.CreateContainer($containerName)
	$blobUtility.UploadFile($containerName,$localFile,$blobName)
	$blobUtility.DownloadBlob($containerName,$localFile,$blobName)
	$contentFile = $blobUtility.DownloadBlobContent($containerName,$localFile,$blobName)
	Write-Host($contentFile)
}
