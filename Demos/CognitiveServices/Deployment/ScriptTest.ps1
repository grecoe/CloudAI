$subscriptionId = "edf507a2-6235-46c5-b560-fd463ba2e771"
$StorageAccountName = "rssimagescgfagl" 
$StorageAccountKey = "Xl/vQfjNcdLybXv98XuCowXeDqxTJp9ay6mIS23QJNthu/f7FAV4xkHL1tCEIRMO6oVUvHgzQX6It3GM/p7Edg=="

$containerName = "acurefunctions"
$websiteFile = "AzureFunctions.zip"
$websitePackage = ".\Functions\AzureFunctions.zip"
# https://rssimagescgfagl.blob.core.windows.net/acurefunctions/host.json
$packageLocation = "https://" + $StorageAccountName + ".blob.core.windows.net/" + $containerName + "/" + $websiteFile

Select-AzureRmSubscription -SubscriptionId $subscriptionId


# create a context for account and key
$ctx=New-AzureStorageContext $StorageAccountName $StorageAccountKey

New-AzureStorageContainer -Name $containerName -Context $ctx -Permission blob

Set-AzureStorageBlobContent -File $websitePackage `
  -Container $containerName `
  -Blob $websiteFile `
  -Context $ctx 
  
  
Write-Host ($packageLocation)