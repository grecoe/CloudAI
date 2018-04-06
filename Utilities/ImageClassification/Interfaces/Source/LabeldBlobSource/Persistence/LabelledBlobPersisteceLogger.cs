//
// Copyright  Microsoft Corporation ("Microsoft").
//
// Microsoft grants you the right to use this software in accordance with your subscription agreement, if any, to use software 
// provided for use with Microsoft Azure ("Subscription Agreement").  All software is licensed, not sold.  
// 
// If you do not have a Subscription Agreement, or at your option if you so choose, Microsoft grants you a nonexclusive, perpetual, 
// royalty-free right to use and modify this software solely for your internal business purposes in connection with Microsoft Azure 
// and other Microsoft products, including but not limited to, Microsoft R Open, Microsoft R Server, and Microsoft SQL Server.  
// 
// Unless otherwise stated in your Subscription Agreement, the following applies.  THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT 
// WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL MICROSOFT OR ITS LICENSORS BE LIABLE 
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THE SAMPLE CODE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.Collections.Generic;
using ImageClassifier.Interfaces.GlobalUtils.AzureStorage;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using ImageClassifier.Interfaces.GlobalUtils.Persistence;

namespace ImageClassifier.Interfaces.Source.LabeldBlobSource.Persistence
{
    /// <summary>
    /// The catalog logger. This separates information from each container into it's own 
    /// catalog file.
    /// </summary>
    class LabelledBlobPersisteceLogger : GenericCsvLogger
    {
        #region Private Members
        private AzureBlobStorageConfiguration Configuration { get; set; }
        public Dictionary<String,String> LabelMap { get; set; }
        #endregion


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

        /// <summary>
        /// Appends, and creates if neccesary, a catalog file for a given image URL.
        /// 
        /// Used when initially acquiring images from the storage account.
        /// </summary>
        public void RecordStorageImage(string container, string url)
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

        /// <summary>
        /// Gets the parsed list of items that are stored in a local catalog file for a particular container.
        /// </summary>
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
                            returnValue.Add(ScoringImage.ParseRecord(entry[0]));
                        }
                    }
                }
            }
            return returnValue;
        }
    }
}
