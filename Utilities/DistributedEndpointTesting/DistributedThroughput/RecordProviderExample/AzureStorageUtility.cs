using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThroughputInterfaces.Configuration;

namespace RecordProviderExample
{
    class AzureStorageUtility
    {
        public const String StorageConnectionStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";

        private RecordProviderStorage Context { get; set; }
        private String ConnectionString { get; set; }
        private CloudStorageAccount StorageAccount { get; set; }
        private CloudBlobClient BlobClient { get; set; }

        public AzureStorageUtility(RecordProviderStorage context)
        {
            this.Context = context;
            this.ConnectionString = String.Format(AzureStorageUtility.StorageConnectionStringFormat,
                this.Context.StorageAccount,
                this.Context.StorageAccountKey);

        }

        public void Connect()
        {
            if (this.StorageAccount == null)
            {
                this.StorageAccount = CloudStorageAccount.Parse(this.ConnectionString);
                this.BlobClient = this.StorageAccount.CreateCloudBlobClient();
            }
        }

        public List<String> ListBlobs(String container, String prefix)
        {
            this.Connect();

            List<String> returnValue = new List<string>();
            if (String.IsNullOrEmpty(container))
            {
                container = this.Context.StorageAccountContainer;
            }

            CloudBlobContainer blobContainer = this.BlobClient.GetContainerReference(container);
            foreach (IListBlobItem item in blobContainer.ListBlobs(prefix, true))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    returnValue.Add(blob.Name);
                }
            }

            return returnValue;
        }
        public List<String> ListBlobs(String container, String prefix, String fileType, int gbMax)
        {
            this.Connect();

            double currentGb = 0;

            List<String> returnValue = new List<string>();
            if (String.IsNullOrEmpty(container))
            {
                container = this.Context.StorageAccountContainer;
            }

            CloudBlobContainer blobContainer = this.BlobClient.GetContainerReference(container);
            foreach (IListBlobItem item in blobContainer.ListBlobs(prefix, true))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    long sizebytes = blob.Properties.Length;
                    currentGb += (double)sizebytes / (double)(1024 * 1024 * 1024);

                    if (!String.IsNullOrEmpty(fileType))
                    {
                        if (blob.Name.EndsWith(fileType))
                        {
                            returnValue.Add(blob.Name);
                        }
                    }
                    else
                    {
                        returnValue.Add(blob.Name);
                    }

                    if ((int)currentGb >= gbMax)
                    {
                        break;
                    }
                }
            }

            return returnValue;
        }
        public List<String> ListBlobs(int fileCountMax, String container, String prefix, String fileType)
        {
            this.Connect();

            List<String> returnValue = new List<string>();
            if (String.IsNullOrEmpty(container))
            {
                container = this.Context.StorageAccountContainer;
            }

            CloudBlobContainer blobContainer = this.BlobClient.GetContainerReference(container);
            foreach (IListBlobItem item in blobContainer.ListBlobs(prefix, true))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;

                    if (!String.IsNullOrEmpty(fileType))
                    {
                        if (blob.Name.EndsWith(fileType))
                        {
                            returnValue.Add(blob.Name);
                        }
                    }
                    else
                    {
                        returnValue.Add(blob.Name);
                    }
                }

                if (returnValue.Count >= fileCountMax)
                {
                    break;
                }
            }

            return returnValue;
        }

        public IEnumerable<KeyValuePair<String, String>> GetBlobs(String container, String prefix, String fileType)
        {
            this.Connect();

            if (String.IsNullOrEmpty(container))
            {
                container = this.Context.StorageAccountContainer;
            }

            CloudBlobContainer blobContainer = this.BlobClient.GetContainerReference(container);
            foreach (IListBlobItem item in blobContainer.ListBlobs(prefix, true))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;

                    if (!String.IsNullOrEmpty(fileType))
                    {
                        if (blob.Name.EndsWith(fileType))
                        {
                            yield return new KeyValuePair<String, String>(System.IO.Path.GetFileName(blob.Name), GetBlobBase64(container, blob.Name));
                        }
                    }
                    else
                    {
                        yield return new KeyValuePair<String, String>(System.IO.Path.GetFileName(blob.Name), GetBlobBase64(container, blob.Name));
                    }
                }
            }

            yield break;
        }

        public String GetBlobBase64(String container, String blob)
        {
            this.Connect();
            String returnValue = String.Empty;

            CloudBlobContainer blobContainer = this.BlobClient.GetContainerReference(container);

            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blob);

            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);
                byte[] image = memoryStream.ToArray();
                returnValue = Convert.ToBase64String(image);
            }

            return returnValue;
        }
    }
}
