using ImageClassifier.Interfaces.GlobalUtils.Configuration;

namespace ImageClassifier.Interfaces.Source.LabelledLocalDisk.Configuration
{
    public class LabelledLocalConfiguration
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "batchSize")]
        public int BatchSize { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "localConfiguration")]
        public LocalDiskSourceConfiguration LocalConfiguration { get; set; }

        public LabelledLocalConfiguration()
        {
            this.LocalConfiguration = new LocalDiskSourceConfiguration();
        }
    }
}
