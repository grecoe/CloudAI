using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordProviderExample
{
    class OverrideConfiguration
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "endpointUrl")]
        public String Url { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "endpointKey")]
        public String Key { get; set; }

        public static OverrideConfiguration LoadOverride()
        {
            OverrideConfiguration returnConfiguration = null;
            String path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Override.json");

            if(System.IO.File.Exists(path))
            {
                returnConfiguration = 
                    Newtonsoft.Json.JsonConvert.DeserializeObject<OverrideConfiguration>(
                        System.IO.File.ReadAllText(path));
            }

            return returnConfiguration;
        }
    }
}
