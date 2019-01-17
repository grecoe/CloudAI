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

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace RssGenerator.RSS
{
    class RSSImageItem
    {
        public String Id { get; set; }
        public String Uri { get; set; }
        public String Name { get; set; }
        public String Path { get; set; }
        public String Hash { get; set; }
    }

    class RSSFeedItem
    {
        public String Id { get; set; }
        public String Title { get; set; }
        public String Summary { get; set; }
        public String PublishedDate { get; set; }
        public String Uri { get; set; }
        public List<RSSImageItem> Images { get; set; }

        public RSSFeedItem()
        {
            this.Images = new List<RSSImageItem>();
        }
    }

    class RSSFeedReader : IDisposable
    {
        #region Private Members
        private bool Disposed { get; set; }
        private const int RETRY_COUNT = 3;
        private String BasePath
        {
            get
            {
                String path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
                if(!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                return path;
            }
        }
        #endregion

        public String FeedUri { get; private set; }

        public RSSFeedReader(String rssFeed)
        {
            this.FeedUri = rssFeed;
        }

        public List<RSSFeedItem> ReadFeed()
        {
            List<RSSFeedItem> returnList = new List<RSSFeedItem>();

            XmlReader reader = XmlReader.Create(this.FeedUri); ;
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            reader.Close();

            foreach (SyndicationItem item in feed.Items)
            {
                RSSFeedItem newItem = new RSSFeedItem();
                newItem.Title = item.Title.Text;
                newItem.Summary = item.Summary.Text;
                newItem.PublishedDate = item.PublishDate.ToString("O");
                newItem.Id = HashGenerator.GetHash(item.Id + DateTime.Now.ToLongTimeString());
                
                foreach (SyndicationLink var in item.Links)
                {
                    if (!String.IsNullOrEmpty(var.MediaType) && var.MediaType.Contains("image"))
                    {
                        newItem.Images.Add(this.DownloadImage(var.Uri.AbsoluteUri).Result);
                    }

                    if(String.IsNullOrEmpty(var.MediaType) && String.Compare(var.RelationshipType,"alternate") == 0 )
                    {
                        newItem.Uri = var.Uri.AbsoluteUri;
                    }
                }
                returnList.Add(newItem);
            }

            return returnList;
        }


        #region Private Helpers
        private void ClearTempFiles()
        {
            foreach(String file in System.IO.Directory.EnumerateFiles(this.BasePath))
            {
                System.IO.File.Delete(file);
            }

            try
            {
                System.IO.Directory.Delete(this.BasePath);
            }
            catch (Exception ex) { }
        }

        private System.Net.Http.HttpClient CreateImageClient(String uri)
        {
            System.Net.Http.HttpClient  imageClient = new System.Net.Http.HttpClient();
            imageClient.BaseAddress = new Uri(uri);
            return imageClient;
        }

        private async Task<RSSImageItem> DownloadImage(String uri)
        {
            RSSImageItem item = new RSSImageItem();
            item.Uri = uri;
            item.Name = System.IO.Path.GetFileName(uri);
            item.Path = System.IO.Path.Combine(this.BasePath, item.Name);
            item.Id = HashGenerator.GetHash(uri);

            if(item.Path.Contains("?"))
            {
                item.Path = item.Path.Substring(0, item.Path.IndexOf('?'));
            }

            using (System.Net.Http.HttpClient imageClient = this.CreateImageClient(uri))
            {
                HttpResponseMessage response = RSSFeedReader.MakeRequest(imageClient, RSSFeedReader.RETRY_COUNT);

                if (response.IsSuccessStatusCode)
                {
                    byte[] arr = await response.Content.ReadAsByteArrayAsync();
                    using (System.IO.FileStream f = System.IO.File.OpenWrite(item.Path))
                    {
                        f.Write(arr, 0, arr.Length);
                    }
                    item.Hash = HashGenerator.GetHash(arr);
                }
            }

            return item;
        }

        public static HttpResponseMessage MakeRequest(HttpClient client, int maxAttempts)
        {
            int attempts = 0;
            HttpResponseMessage returnMessage;

            // Create the request
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, string.Empty);

            // Make request
            returnMessage = client.SendAsync(request).Result;
            while (!returnMessage.IsSuccessStatusCode && attempts < maxAttempts)
            {
                // Give it a few clicks to see if we are in trouble
                System.Threading.Thread.Sleep(10);

                // Can't send same request twice, so build another....
                request = new HttpRequestMessage(HttpMethod.Get, string.Empty);

                attempts++;
                returnMessage = client.SendAsync(request).Result;
            }

            return returnMessage;
        }

        #endregion

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (this.Disposed)
                return;

            if (disposing)
            {
                this.ClearTempFiles();
            }

            this.Disposed = true;
        }
        #endregion

    }
}
