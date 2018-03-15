using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputApp.Configuration
{
    public class ExecutionConfiguration
    {
        /// <summary>
        /// Starting thread count
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "threadCount")]
        public int ThreadCount { get; set; }

        /// <summary>
        /// Thread count increment
        /// Set to 0 if only one thread count type is required
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "threadStep")]
        public int ThreadStep { get; set; }

        /// <summary>
        /// Max thread count.
        /// Set to ThreadCount to 
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "maxThreadCount")]
        public int MaxThreadCount { get; set; }

        /// <summary>
        /// How many iterations for the number of records per thread count.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "testCountPerThreadStep")]
        public int TestCountPerThreadStep { get; set; }

        /// <summary>
        /// Number of times the network call should be retried before marking it as failure
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "retryCount")]
        public int RetryCount { get; set; }

        /// <summary>
        /// Name to put in both HTTP requests to service as user_agent adn the client name
        /// to be used in the event hub settings.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "clientName")]
        public string ClientName { get; set; }


        [Newtonsoft.Json.JsonProperty(PropertyName = "autoScaling")]
        public bool AutoScaling { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "autoScaleIncrement")]
        public int AutoScaleIncrement { get; set; }
    }
}
