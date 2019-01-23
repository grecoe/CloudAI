using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SubDataUpload.DataFormat;

namespace SubDataUpload.Config
{
    class Configuration
    {
        #region Non-JSON Related
        [JsonIgnore]
        private const String CONFIG_FILE = "Configuration.json";
        [JsonIgnore]
        private const String DATA_FILE = "subscription_overview.json";
        #endregion

        [JsonProperty(PropertyName = "storageAccountName")]
        public String StorageAccountName { get; set; }

        [JsonProperty(PropertyName = "storageAccountKey")]
        public String StorageAccountKey { get; set; }

        [JsonProperty(PropertyName = "sourceDirectory")]
        public String SourceDirectory { get; set; }

        [JsonProperty(PropertyName = "latestDataContainer")]
        public String LatestDataContainer { get; set; }

        
        protected Configuration()
        {

        }

        public static Configuration LoadConfiguration()
        {
            String path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
            String content = System.IO.File.ReadAllText(path);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(content);
        }

        public List<SubscriptionData> CollectSubscriptionInformation()
        {
            List<SubscriptionData> returnData = new List<SubscriptionData>();
            if(System.IO.Directory.Exists(this.SourceDirectory) == false)
            {
                throw new Exception("Source directory is invalid");
            }

            foreach(String dir in System.IO.Directory.GetDirectories(this.SourceDirectory))
            {
                foreach(String file in System.IO.Directory.GetFiles(dir))
                {
                    if(String.Compare(System.IO.Path.GetFileName(file), DATA_FILE, true) == 0)
                    {
                        SubscriptionData data = Newtonsoft.Json.JsonConvert.DeserializeObject<SubscriptionData>(System.IO.File.ReadAllText(file));
                        data.GroupData.Regions = this.SortDictionary(data.GroupData.Regions);
                        data.Resources = this.SortDictionary(data.Resources);
                        data.SubscriptionId = System.IO.Path.GetFileName(dir);

                        returnData.Add(data);
                    }
                }
            }

            return returnData;
        }

        private Dictionary<String,int> SortDictionary(Dictionary<string,int> original)
        {
            // Create list of KeyValuePairs and sort on value
            var myList = original.ToList();
            myList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

            return myList.OrderByDescending(key => key.Value).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
