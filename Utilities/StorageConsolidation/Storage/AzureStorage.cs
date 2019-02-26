//
// Copyright  Microsoft Corporation ("Microsoft").
//
// Microsoft grants you the right to use this software in accordance with your subscription agreement, if any, to use software 
// provided for use with Microsoft Azure ("Subscription Agreement").  All software is licensed, not sold.  
// 
// If you do not have a Subscription Agreement, or at your option if you so choose, Microsoft grants you a nonexclusive, perpetual, 
// royalty-free right to use and modify this software solely for your internal business purposes in connection with Microsoft Azure 
// and other Microsoft products, including but not limited to, Microsoft R Open, Microsoft R Server, and Microsoft SQL Server.  
// 
// Unless otherwise stated in your Subscription Agreement, the following applies.  THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT 
// WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL MICROSOFT OR ITS LICENSORS BE LIABLE 
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THE SAMPLE CODE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StorageConsolidation.Storage
{
    /// <summary>
    /// Enumerator used throughout the application to determine whether the caller is working
    /// with Blob Storage or File Shares.
    /// </summary>
    enum StorageType
    {
        BlobContainer = 1,
        FileShare
    }

    /// <summary>
    /// Generic class that encapsulates the name and URI of either an Blob Container 
    /// or File Share
    /// </summary>
    class StorageLocation
    {
        public StorageType StorageType { get; set; }
        public String Name { get; set; }
        public String Uri { get; set; }
    }

    /// <summary>
    /// Helper class that encapsulates the use of Azure Storage.
    /// </summary>
    class AzureStorage
    {
        #region Private Consts/Members
        private String ConnectionString { get; set; }
        private Dictionary<string,string> ConnectionStringParts { get; set; }
        private CloudStorageAccount StorageAccount { get; set; }
        private CloudBlobClient BlobClient { get; set; }
        private CloudFileClient FileClient { get; set;  }
        #endregion

        #region Properties
        public String AccountName
        {
            get
            {
                return this.ConnectionStringParts[ConnectionStringParser.ACCOUNT_NAME];
            }
        }
        public String AccountKey
        {
            get
            {
                return this.ConnectionStringParts[ConnectionStringParser.ACCOUNT_KEY];
            }
        }
        #endregion

        public AzureStorage(String storageConnectionString)
        {
            this.ConnectionString = storageConnectionString;
            this.ConnectionStringParts = ConnectionStringParser.ParseConnectionString(this.ConnectionString);
        }

        /// <summary>
        /// Gets a list of Blob Containers or File Shares from the acccount
        /// </summary>
        /// <param name="storageType">Either blob or file share</param>
        /// <returns>List of location names and URI's</returns>
        public List<StorageLocation> GetStorageLocations(StorageType storageType)
        {
            this.Connect();
            List<StorageLocation> returnData = new List<StorageLocation>();
            switch(storageType)
            {
                case StorageType.BlobContainer:
                    returnData = this.GetContainers();
                    break;
                case StorageType.FileShare:
                    returnData = this.GetFileShares();
                    break;
                default:
                    throw new Exception("Unsupported storage type");
            }

            return returnData;
        }

        /// <summary>
        /// Create either a Blob Container or File Share in the storage account
        /// </summary>
        /// <param name="storageType">Blob or FileShare</param>
        /// <param name="name">Name of item to create</param>
        /// <returns>True if created.</returns>
        public bool CreateStorageLocation(StorageType storageType, String name)
        {
            this.Connect();
            object result = null;

            switch (storageType)
            {
                case StorageType.BlobContainer:
                    result = this.CreateContainer(name).Result;
                    break;
                case StorageType.FileShare:
                    result = this.CreateFileShare(name);
                    break;
                default:
                    throw new Exception("Unsupported storage type");
            }

            return (result != null);
        }

        /// <summary>
        /// Determine if a Blob Container or File Share exists in the account
        /// </summary>
        /// <param name="storageType">Blob or FileShare</param>
        /// <param name="name">Name to check</param>
        /// <returns>True if the item exists, false otherwise.</returns>
        public bool StorageLocationExists(StorageType storageType, String name)
        {
            this.Connect();
            bool result = false;

            switch (storageType)
            {
                case StorageType.BlobContainer:
                    result = this.ContainerExists(name);
                    break;
                case StorageType.FileShare:
                    result = this.FileShareExists(name);
                    break;
                default:
                    throw new Exception("Unsupported storage type");
            }

            return result;
        }

        #region Container Helpers
        /// <summary>
        /// Connects to the storage account
        /// </summary>
        private void Connect()
        {
            if (this.StorageAccount == null)
            {
                this.StorageAccount = CloudStorageAccount.Parse(this.ConnectionString);
                this.BlobClient = this.StorageAccount.CreateCloudBlobClient();
                this.FileClient = this.StorageAccount.CreateCloudFileClient();
            }
        }

        /// <summary>
        /// Gets a list of storage containers from the specified account
        /// </summary>
        private List<StorageLocation> GetContainers()
        {
            List<StorageLocation> returnContainers = new List<StorageLocation>();

            foreach(CloudBlobContainer container in this.BlobClient.ListContainers())
            {
                StorageLocation newContainer = new StorageLocation();
                newContainer.Name = container.Name;
                newContainer.Uri= container.Uri.ToString();
                newContainer.StorageType = StorageType.BlobContainer;

                returnContainers.Add(newContainer);
            }

            return returnContainers;
        }

        /// <summary>
        /// Create a storage container
        /// </summary>
        /// <param name="containerName">Container name, limit is 63 characters</param>
        private async Task<CloudBlobContainer> CreateContainer(String containerName)
        {
            CloudBlobContainer cloudBlobContainer = null;
            if (!this.ContainerExists(containerName))
            {
                cloudBlobContainer = this.BlobClient.GetContainerReference(containerName);
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

        /// <summary>
        /// Determine if a blob storage container exists
        /// </summary>
        /// <param name="containerName">Name of container to check</param>
        private bool ContainerExists(String containerName)
        {
            CloudBlobContainer cloudBlobContainer = this.BlobClient.GetContainerReference(containerName);
            return cloudBlobContainer.Exists();
        }
        #endregion

        #region File Share Helpers
        /// <summary>
        /// Gets a list of file shares from the specified account
        /// </summary>
        private List<StorageLocation> GetFileShares()
        {
            List<StorageLocation> returnShares = new List<StorageLocation>();

            foreach(CloudFileShare share in this.FileClient.ListShares())
            {
                StorageLocation newShare = new StorageLocation();
                newShare.Name = share.Name;
                newShare.Uri = share.Uri.ToString();
                newShare.StorageType = StorageType.FileShare;
                returnShares.Add(newShare);
            }

            return returnShares;
        }

        /// <summary>
        /// Create a new File Share
        /// </summary>
        /// <param name="shareName">Name of the share to create</param>
        /// <returns></returns>
        private async Task<CloudFileShare> CreateFileShare(String shareName)
        {
            CloudFileShare fileShare = null;
            if (!this.FileShareExists(shareName))
            {
                fileShare = this.FileClient.GetShareReference(shareName);
                await fileShare.CreateAsync();
            }

            return fileShare;
        }

        /// <summary>
        /// Determine if a Azure File Share exists
        /// </summary>
        /// <param name="shareName">Name of the share to validate</param>
        private bool FileShareExists(String shareName)
        {
            CloudFileShare share = this.FileClient.GetShareReference(shareName);
            return share.Exists();
        }
        #endregion

    }
}
