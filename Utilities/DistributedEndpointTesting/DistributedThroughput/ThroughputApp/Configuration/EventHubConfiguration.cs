using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputApp.Configuration
{
    public class EventHubConfiguration
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "hubName")]
        public String EventHubName { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "connectionString")]
        public String ServiceBusConnectionString { get; set; }
    }
}
