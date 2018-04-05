using System;

namespace ThroughputApp.Configuration
{
    public class DefaultProviderConfiguration
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "endpointUrl")]
        public String Url { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "endpointKey")]
        public String Key { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "recordCount")]
        public int RecordCount { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "fileInput")]
        public String File { get; set; }

        public bool IsValid()
        {
            bool returnValue = false;

            if(!String.IsNullOrEmpty(this.File))
            {
                if(System.IO.File.Exists(this.File))
                {
                    //returnValue = !String.IsNullOrEmpty(this.Url) &&
                    //    !String.IsNullOrEmpty(this.Key);
                    returnValue = !String.IsNullOrEmpty(this.Url);
                }
            }

            return returnValue;
        }
    }
}
