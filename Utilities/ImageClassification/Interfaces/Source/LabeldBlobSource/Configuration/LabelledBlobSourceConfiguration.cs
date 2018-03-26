using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageClassifier.Interfaces.Source.LabeldBlobSource.Configuration
{
    public class LabelledBlobSourceConfiguration
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "batchSize")]
        public int BatchSize { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "storageConfiguration")]
        public AzureBlobStorageConfiguration StorageConfiguration { get; set; }

        public LabelledBlobSourceConfiguration()
        {
            this.StorageConfiguration = new AzureBlobStorageConfiguration();
        }

    }
}
