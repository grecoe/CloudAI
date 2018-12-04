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
using System.Threading.Tasks;

namespace RssGenerator.StorageHelper
{
    // https://dangtestrepo.blob.core.windows.net/scraped/ac.JPG
    class AzureStorageUtility
    {
        private CloudStorageAccount StorageAccount { get; set; }
        private CloudBlobClient BlobClient { get; set; }
        private CloudBlobContainer BlobContainer { get; set; }

        public String ConnectionString { get; private set;}
        public String Container { get; private set; }

        public AzureStorageUtility(String connection, String container)
        {
            this.ConnectionString = connection;
            this.Container = container;

            this.StorageAccount = CloudStorageAccount.Parse(this.ConnectionString);
            this.BlobClient = this.StorageAccount.CreateCloudBlobClient();

            this.BlobContainer = this.BlobClient.GetContainerReference(this.Container);
            if(!this.BlobContainer.Exists())
            {
                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };

                this.BlobContainer.Create();
                this.BlobContainer.SetPermissions(permissions);

            }
        }

        public async Task<String> UploadBlob(String path)
        {
            String returnValue = String.Empty;

            CloudBlockBlob cloudBlockBlob = this.BlobContainer.GetBlockBlobReference(System.IO.Path.GetFileName(path));
            await cloudBlockBlob.UploadFromFileAsync(path);

            return cloudBlockBlob.Uri.AbsoluteUri;
        }
    }
}
