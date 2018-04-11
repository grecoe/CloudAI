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
using System;
using System.Collections.Generic;
using System.IO;
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
