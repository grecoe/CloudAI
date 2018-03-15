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

using ImageClassifier.Interfaces.GlobalUtils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageClassifier.Interfaces.Sink.Catalog
{
    class CollectionData
    {
        public List<ScoredItem> Items { get; set; }
        public GenericCsvLogger Logger { get; set; }

        public CollectionData()
        {
            this.Items = new List<ScoredItem>();
        }
    }

    // Creates a catalog directory right under the 
    // main application running directory called catalog.
    class CatalogSink : GenericCsvLogger, IDataSink
    {
        private const String BASEDIR = "Catalog";
        private const String BASECOLLECTION = "Catalog.csv";
        private static string[] COLLECTION_HEADERS = new string[] { "Container,Location" };

        private static string[] CATALOG_HEADERS = new string[] { "Container,Item,Classifications" };

        /// <summary>
        /// Collection of the original conatiner to the container CSV
        /// map that this object collects.
        /// </summary>
        private Dictionary<string,string> CollectionMap { get; set; }

        private Dictionary<string, CollectionData> Collections { get; set; }

        public CatalogSink(string provider)
            :base(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, String.Format("{0}{1}", provider,CatalogSink.BASEDIR)),
                 CatalogSink.BASECOLLECTION,
                 CatalogSink.COLLECTION_HEADERS)
        {
            this.Name = "Catalog Sink";
            // Get container mapping
            this.Collections = new Dictionary<string, CollectionData>();
            this.CollectionMap = new Dictionary<string, string>();
            foreach(string[] entry in this.GetEntries())
            {
                if(entry.Count() == 2)
                {
                    this.CollectionMap.Add(entry[0], entry[1]);
                }
            }

            // Get container data
            this.LoadData();
        }

        #region IDataSink
        public String Name { get; private set; }

        public ScoredItem Find(string container, string name)
        {
            ScoredItem returnItem = null;
            if (this.CollectionMap.ContainsKey(container))
            {
                CollectionData collection = GetCollection(container);

                if (collection != null)
                {
                    //Is this a duplicate? Are we updating something?
                    returnItem = collection.Items.FirstOrDefault(x => String.Compare(x.Name, name) == 0);
                }
            }
            return returnItem;
        }

        public bool ItemHasBeenScored(string container, string name)
        {
            bool returnValue = false;
            // Get or create the collection
            CollectionData collection = GetCollection(container);
            if(collection != null)
            {
                //Is this a duplicate? Are we updating something?
                returnValue = (collection.Items.FirstOrDefault(x => String.Compare(x.Name, name) == 0) != null);

            }
            return returnValue;
        }

        public void Record(ScoredItem item)
        {
            // Get or create the collection
            CollectionData collection = GetCollection(item.Container);

            if (collection != null)
            {
                //Is this a duplicate? Are we updating something?
                ScoredItem existing = collection.Items.FirstOrDefault(x => String.Compare(x.Name, item.Name) == 0);
                if (existing != null)
                {
                    existing.Classifications = item.Classifications;

                    // Update the file
                    collection.Logger.ClearLog();

                    // Rewrite the contents
                    foreach (ScoredItem colitem in collection.Items)
                    {
                        collection.Logger.Record(this.FormatItem(colitem));
                    }
                }
                else
                {
                    collection.Items.Add(item);
                    collection.Logger.Record(this.FormatItem(item));
                }
            }
        }

        public void Purge()
        {
            this.Collections = new Dictionary<string, CollectionData>();
            this.CollectionMap = new Dictionary<string, string>();
            FileUtils.DeleteFiles(this.Directory, new string[] { "*.csv" });
        }

        #endregion

        private String FormatItem(ScoredItem item)
        {
            return String.Format("{0},{1},{2}",
                            item.Container,
                            item.Name,
                            String.Join("|", item.Classifications));
        }
        private CollectionData GetCollection(string container)
        {
            CollectionData returnData = null;
            if (!String.IsNullOrEmpty(container))
            {
                if (!this.CollectionMap.ContainsKey(container))
                {
                    String fileName = String.Format("{0}.csv", Guid.NewGuid().ToString("N"));
                    fileName = System.IO.Path.Combine(this.Directory, fileName);

                    // Add to the collection and record it
                    this.CollectionMap.Add(container, fileName);
                    this.Record(new string[] { container, fileName });
                }

                if (!this.Collections.ContainsKey(container))
                {
                    this.Collections.Add(container, new CollectionData());

                    this.Collections[container].Logger = new GenericCsvLogger(
                        System.IO.Path.GetDirectoryName(this.CollectionMap[container]),
                        System.IO.Path.GetFileName(this.CollectionMap[container]),
                        CatalogSink.CATALOG_HEADERS);
                }

                returnData = this.Collections[container];
            }
            return returnData;
        }

        private void LoadData()
        {
            foreach(KeyValuePair <string,string> kvp in this.CollectionMap )
            {
                this.Collections.Add(kvp.Key, new CollectionData());

                this.Collections[kvp.Key].Logger = new GenericCsvLogger(
                    System.IO.Path.GetDirectoryName(kvp.Value),
                    System.IO.Path.GetFileName(kvp.Value),
                    CatalogSink.CATALOG_HEADERS);

                foreach(string[] entry in this.Collections[kvp.Key].Logger.GetEntries())
                {
                    if (entry.Length == 3)
                    {
                        this.Collections[kvp.Key].Items.Add(new ScoredItem()
                        {
                            Container = entry[0],
                            Name = entry[1],
                            Classifications = new List<string>(entry[2].Split(new char[] { '|' }))
                        });
                    }
                }
            }
        }
    }
}
