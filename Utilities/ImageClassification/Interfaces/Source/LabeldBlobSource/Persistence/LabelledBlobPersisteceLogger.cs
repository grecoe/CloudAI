using ImageClassifier.Interfaces.GlobalUtils;
using ImageClassifier.Interfaces.GlobalUtils.AzureStorage;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using ImageClassifier.Interfaces.Source.BlobSource.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageClassifier.Interfaces.Source.LabeldBlobSource.Persistence
{
    /// <summary>
    /// Different from the single instance, we need to keep images separated by
    /// container/directory so we use this class here as a master dictionary of the other 
    /// files.
    /// </summary>
    class LabelledBlobPersisteceLogger : GenericCsvLogger
    {
        private AzureBlobStorageConfiguration Configuration { get; set; }
        public Dictionary<String,String> LabelMap { get; set; }

        public LabelledBlobPersisteceLogger(AzureBlobStorageConfiguration configuration)
            : base(configuration.RecordLocation,
                String.Format("{0}LabelDictionary.csv", configuration.StorageAccount),
                new string[] { "Label","Csv" })
        {
            this.Configuration = configuration;

            this.LabelMap = new Dictionary<string, string>();
            foreach(string[] entry in this.GetEntries())
            {
                if(entry.Length == 2)
                {
                    this.LabelMap.Add(entry[0], entry[1]);
                }
            }
        }

        public void RecordLabelledImage(string container, string url)
        {
            if(String.IsNullOrEmpty(container) || string.IsNullOrEmpty(url))
            {
                throw new ArgumentException();
            }

            if(!this.LabelMap.ContainsKey(container))
            {
                this.LabelMap.Add(container, String.Format("{0}.csv", Guid.NewGuid().ToString("N")));
                this.Record(new string[] { container, this.LabelMap[container] });
            }

            GenericCsvLogger labelLogger = new GenericCsvLogger(
                this.Configuration.RecordLocation,
                this.LabelMap[container],
                new string[] { "Url" });

            labelLogger.Record(url);
        }

        public List<ScoringImage> LoadContainerData(string container)
        {
            List<ScoringImage> returnValue = new List<ScoringImage>();

            if (!String.IsNullOrEmpty(container))
            {
                if (this.LabelMap.ContainsKey(container))
                {
                    GenericCsvLogger labelLogger = new GenericCsvLogger(
                        this.Configuration.RecordLocation,
                        this.LabelMap[container],
                        new string[] { "Url" });

                    foreach (string[] entry in labelLogger.GetEntries())
                    {
                        if (entry.Length == 1)
                        {
                            returnValue.Add(ParseRecord(entry[0]));
                        }
                    }
                }
            }
            return returnValue;
        }

        public static ScoringImage ParseRecord(String entry)
        {
            string[] parts = entry.Split(new char[] { ',' });

            if (parts.Length == 1)
            {
                return new ScoringImage()
                {
                    Url = parts[0]
                };
            }

            return null;
        }


    }
}
