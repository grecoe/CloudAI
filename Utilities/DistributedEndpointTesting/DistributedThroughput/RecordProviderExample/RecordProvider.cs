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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using ThroughputInterfaces;
using ThroughputInterfaces.Configuration;

namespace RecordProviderExample
{
    public class RecordProvider : IRecordProvider
    {
        // No real record id's so just track them
        private static int RecordId = 0;

        private OverrideConfiguration Configuration { get; set; }

        /// <summary>
        /// Returns the URL of the endpoint that is going to be load tested
        /// </summary>
        public String EndpointUrl
        {
            get
            {
                if (this.Configuration != null)
                {
                    return this.Configuration.Url;
                }
                return "[YOUR_SCORING_ENDPOINT]";
            }
        }

        /// <summary>
        /// Returns the API key of the endpoint that is goign to be tested
        /// </summary>
        public String EndpointKey
        {
            get
            {
                if(this.Configuration != null)
                {
                    return this.Configuration.Key;
                }
                return "[YOUR_SCORING_ENDPOINT_KEY]";
            }
        }

        public RecordProvider()
        {
            this.Configuration = OverrideConfiguration.LoadOverride();
        }

        /// <summary>
        /// Return a list of objects that match your API input. This one returns a simple JSON object for an image detection
        /// API.
        /// 
        /// There are currently two options in the RecordProviderConfiguration : "storage" or "local", but in reality in the check
        /// below, if we don't find storage, we assume local.
        /// 
        /// "storage" in this case means records will be loaded from Azure Storage using the information in RecordProviderConfiguration.Storage
        /// object.
        /// </summary>
        public IDictionary<int, object> LoadRecords(RecordProviderConfiguration configuration, OnStatusUpdate onStatus = null)
        {
            Dictionary<int, object> returnObjects = new Dictionary<int, object>();

            if (String.Compare(configuration.ExecutionType, "storage", true) == 0)
            {
                AzureStorageUtility storageUtility = new AzureStorageUtility(configuration.Storage);
                List<String> blobList = storageUtility.ListBlobs(
                        configuration.RecordCount,
                        configuration.Storage.StorageAccountContainer,
                        configuration.Storage.BlobPrefix,
                        configuration.Storage.FileType);

                foreach (String blob in blobList)
                {
                    String content = storageUtility.GetBlobBase64(configuration.Storage.StorageAccountContainer, blob);
                    returnObjects.Add(RecordId++, PackageRecord(System.IO.Path.GetFileName(blob), content));
                }


                onStatus?.Invoke($"Loaded {returnObjects.Count} records from storage.");
            }
            else
            {
                // Loading from file - 
                String fileToLoad = configuration.Local.LocalFile;
                if (System.IO.File.Exists(fileToLoad))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(fileToLoad);
                    String content = Convert.ToBase64String(fileData);
                    String name = System.IO.Path.GetFileName(fileToLoad);

                    for (int i = 0; i < configuration.RecordCount; i++)
                    {
                        returnObjects.Add(RecordId++,PackageRecord(String.Format("{0}_{1}", i, name), content));
                    }

                    onStatus?.Invoke($"Created {returnObjects.Count} records from local system.");
                }
            }

            return returnObjects;
        }

        private String PackageRecord(String fileName, String content)
        {
            JObject rss = new JObject(new JProperty(fileName, content));
            return rss.ToString(Newtonsoft.Json.Formatting.Indented);
        }
    }
}
