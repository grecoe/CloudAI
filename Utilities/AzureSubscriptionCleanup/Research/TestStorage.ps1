################################################################
#	Test storage
################################################################

. './Azure/Subscription.ps1'
. './Azure/Storage.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

# Provide your subscription to verify
$subId = 'YOUR_SUBSCRIPTION_KEY' 
$account='Storage_Account_Name'
$key='Storage_Access_Key'

$containerName = 'foobar'
$localFile ='.\testfile.txt'
$blobName='foo.json'
$downloadLocalFile ='.\testfile2.txt'

# Generate some information and write it to a file
$randomData = @{}
$randomData.Add("first","data")
Out-File -FilePath $localFile -InputObject ($randomData | ConvertTo-Json -depth 100)


# Set context so we don't get hung up later
SetContext -subId $subId

# Get the storage context
$ctx = CreateContext -acctName $account -accessKey $key

# Create a container
$createResult = CreateContainer -ctx $ctx -name $containerName

# Upload to blob
$uploadResult = UploadLocalFile -ctx $ctx -container $containerName -localPath $localFile -blobName $blobName

# If upload succeeds, download it as text
if($uploadResult)
{
	$result = DownloadBlob -ctx $ctx -container $containerName -localPath $downloadLocalFile -blobName $blobName -getText
	Write-Host("File Content:")
	Write-Host($result)
}
