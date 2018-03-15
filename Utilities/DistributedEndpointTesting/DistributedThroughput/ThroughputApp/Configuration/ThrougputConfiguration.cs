using System;
using ThroughputInterfaces.Configuration;

namespace ThroughputApp.Configuration
{
    public class ThroughputConfiguration
    {
        private const String ConfigurationFile = "ThroughputConfiguration.json";

        [Newtonsoft.Json.JsonIgnore]
        public int SelectedRecordCount { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "recordProviderDiskLocation")]
        public String RecordProviderDiskLocation { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "logDirectory")]
        public String LogDirectory { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "recordProvider")]
        public RecordProviderConfiguration RecordConfiguration { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "execution")]
        public ExecutionConfiguration Execution { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "defaultProvider")]
        public DefaultProviderConfiguration DefaultProvider { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "eventHubConfiguration")]
        public EventHubConfiguration HubConfiguration { get; set; }
        
        protected ThroughputConfiguration()
        {
            this.RecordConfiguration = new RecordProviderConfiguration();
            this.Execution = new ExecutionConfiguration();
            this.DefaultProvider = new DefaultProviderConfiguration();
        }

        public static ThroughputConfiguration LoadConfiguration()
        {
            String path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ThroughputConfiguration.ConfigurationFile);
            String content = System.IO.File.ReadAllText(path);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<ThroughputConfiguration>(content);
        }
    }
}
