using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputInterfaces.Configuration
{
    public class RecordProviderConfiguration
    {
        /// <summary>
        /// Execution type can be 
        ///     storage: The RecordProviderStorage contains the information about a storage account
        ///              in which to locate records to use for the engine.
        ///     local : The RecordProviderLocal contains the information about a single local file to be iterated
        ///             on during execution
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "executionType")]
        public String ExecutionType { get; set; }

        /// <summary>
        /// The number of records that will be returned by the interface to iterate over during testing.
        /// This number of records will be sent over the configured number of threads from the main application.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "recordCount")]
        public int RecordCount { get; set; }

        /// <summary>
        /// Azure blob storage account information 
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "storage")]
        public RecordProviderStorage Storage { get; set; }

        /// <summary>
        /// Local file path
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "local")]
        public RecordProviderLocal Local { get; set; }

        public RecordProviderConfiguration()
        {
            this.Storage = new RecordProviderStorage();
            this.Local = new RecordProviderLocal();
        }
    }
}
