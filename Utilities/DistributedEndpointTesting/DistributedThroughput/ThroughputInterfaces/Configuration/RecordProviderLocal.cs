using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputInterfaces.Configuration
{
    public class RecordProviderLocal
    {
        /// <summary>
        /// When 
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "localFile")]
        public String LocalFile { get; set; }
    }
}
