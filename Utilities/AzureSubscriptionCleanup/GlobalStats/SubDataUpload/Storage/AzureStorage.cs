using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SubDataUpload.Config;
using SubDataUpload.DataFormat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubDataUpload.Storage
{
    class AzureStorage
    {
        #region Private Consts/Members
        private const String StorageConnectionStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";
        private Configuration Configuration { get; set; }
        private String ConnectionString { get; set; }
        private CloudStorageAccount StorageAccount { get; set; }
        private CloudBlobClient BlobClient { get; set; }
        #endregion

        public AzureStorage(Configuration config)
        {
            this.Configuration = config;
            this.ConnectionString = String.Format(StorageConnectionStringFormat, this.Configuration.StorageAccountName, this.Configuration.StorageAccountKey);
        }

        /// <summary>
        /// Connects to the storage account
        /// </summary>
        public void Connect()
        {
            if (this.StorageAccount == null)
            {
                this.StorageAccount = CloudStorageAccount.Parse(this.ConnectionString);
                this.BlobClient = this.StorageAccount.CreateCloudBlobClient();
            }
        }

        public async Task<bool> UploadLatestData(List<SubscriptionData> dataList)
        {
            this.Connect();

            // Ditch the latest data (already archived)
            bool deleteStatus = this.DeleteContainer(this.Configuration.LatestDataContainer).Result;

            // Create the containers
            String containerName = DateTime.Now.ToString("dd-MMMM-yyyy").ToLower();
            CloudBlobContainer newContainer = this.CreateContainer(containerName).Result;
            CloudBlobContainer latestContainer = this.CreateContainer(this.Configuration.LatestDataContainer).Result;

            foreach (SubscriptionData data in dataList)
            {
                // Create the content to upload
                String fileName = String.Format("{0}.json", data.SubscriptionId);
                String fileContent = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

                // Create the blob in both the archive and latest paths.
                CloudBlockBlob archiveBlob = newContainer.GetBlockBlobReference(fileName);
                await archiveBlob.UploadTextAsync(fileContent);

                CloudBlockBlob latestBlob = latestContainer.GetBlockBlobReference(fileName);
                await latestBlob.UploadTextAsync(fileContent);
            }

            return true;
        }

        private async Task<CloudBlobContainer> CreateContainer(String containerName)
        {
            CloudBlobContainer cloudBlobContainer = this.BlobClient.GetContainerReference(containerName);
            if (cloudBlobContainer.Exists() == false)
            {
                await cloudBlobContainer.CreateAsync();

                // Set the permissions so the blobs are public. 
                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                await cloudBlobContainer.SetPermissionsAsync(permissions);
            }

            return cloudBlobContainer;
        }

        private async Task<bool> DeleteContainer(String containerName)
        {
            bool returnResult = true;
            CloudBlobContainer cloudBlobContainer = this.BlobClient.GetContainerReference(containerName);

            if (cloudBlobContainer.Exists() )
            {
                try
                {
                    await cloudBlobContainer.DeleteAsync();
                }
                catch(Exception ex)
                {
                    returnResult = false;
                }
            }

            return returnResult;
        }

    }
}
