using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputInterfaces.Configuration
{
    public class RecordProviderStorage
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "storageAccount")]
        public String StorageAccount { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "storageKey")]
        public String StorageAccountKey { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "storageContainer")]
        public String StorageAccountContainer { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "blobPrefix")]
        public String BlobPrefix { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "fileType")]
        public String FileType { get; set; }
    }
}
