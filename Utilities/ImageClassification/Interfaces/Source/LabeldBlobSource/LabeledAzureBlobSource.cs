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

using ImageClassifier.Interfaces.GlobalUtils.AzureStorage;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using ImageClassifier.Interfaces.Source.LabeldBlobSource.Persistence;
using System;
using System.Linq;
using System.Collections.Generic;
using ImageClassifier.Interfaces.GlobalUtils;
using System.Windows;
using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.Source.LabeldBlobSource.Configuration;
using ImageClassifier.Interfaces.Source.LabeldBlobSource.UI;

namespace ImageClassifier.Interfaces.Source.LabeldBlobSource
{
    class LabeledAzureBlobSource : DataSourceBase<LabelledBlobSourceConfiguration>, IMultiImageDataSource 
    {
        #region PrivateMembers
        private const int DEFAULT_BATCH_SIZE = 6;

        /// <summary>
        /// Custom Configuration that includes additional settings over and above the AzureStorageConfiguration
        /// </summary>
        private LabelledBlobSourceConfiguration Configuration { get; set; }
        /// <summary>
        /// Utility to read Azure Storage account
        /// </summary>
        private StorageUtility AzureStorageUtils { get; set; }
        /// <summary>
        /// Persists storage information account information
        /// </summary>
        private LabelledBlobPersisteceLogger PersistenceLogger { get; set; }
        /// <summary>
        /// Index into CurrentImageList
        /// </summary>
        private int CurrentImage { get; set; }
        /// <summary>
        /// List of files from the currently selected catalog file
        /// </summary>
        private List<ScoringImage> CurrentImageList { get; set; }
        #endregion

        public LabeledAzureBlobSource()
            : base("LabeledAzureStorageConfiguration.json")
        {
            this.Name = "LabelledAzureStorageSource";
            this.SourceType = DataSourceType.LabelledBlob;
            this.DeleteSourceFilesWhenComplete = true;
            this.MultiClass = true;
            this.CurrentImage = -1;


            // Get the configuration specific to this instance
            this.Configuration = this.LoadConfiguration();

            if(this.Configuration.BatchSize <= 0 || this.Configuration.BatchSize > 9)
            {
                this.Configuration.BatchSize = LabeledAzureBlobSource.DEFAULT_BATCH_SIZE;
                this.SaveConfiguration(this.Configuration);
            }

            // Create the storage utils
            this.AzureStorageUtils = new StorageUtility(this.Configuration.StorageConfiguration);

            // Prepare the UI control with the right hooks.
            CustomStorageConfiguration configUi = new CustomStorageConfiguration(this, this.Configuration);
            configUi.OnConfigurationSaved += ConfigurationSaved;
            configUi.OnSourceDataUpdated += AcquireContent;

            this.ConfigurationControl =
                new ConfigurationControlImpl("Labelled Azure Storage Service",
                configUi);

            // Get a list of containers through the persistence logger 
            this.PersistenceLogger = new LabelledBlobPersisteceLogger(this.Configuration.StorageConfiguration);
            this.CurrentContainer = this.Containers.FirstOrDefault();
            this.InitializeOnNewContainer();

            this.ContainerControl = new GenericContainerControl(this);
            this.ImageControl = new MultiImageControl(this);
        }

        #region IMultiImageDataSource

        public event OnContainerLabelsAcquired OnLabelsAcquired;
        public int BatchSize { get { return this.Configuration.BatchSize; } }

        public IEnumerable<SourceFile> NextSourceGroup()
        {
            List<SourceFile> returnFiles = new List<SourceFile>();
            if (this.CanMoveNext)
            {
                if (this.CurrentImage <= -1)
                {
                    this.CurrentImage = -1;
                }

                int count = 0;
                while (this.CanMoveNext && count++ < this.BatchSize)
                {
                    ScoringImage image = this.CurrentImageList[++this.CurrentImage];

                    // Download blah blah
                    String token = this.AzureStorageUtils.GetSasToken(this.Configuration.StorageConfiguration.StorageContainer);
                    String imageUrl = String.Format("{0}{1}", image.Url, token);

                    SourceFile returnFile = new SourceFile();
                    returnFile.Name = System.IO.Path.GetFileName(image.Url);
                    returnFile.DiskLocation = this.DownloadStorageFile(imageUrl);

                    if (this.Sink != null)
                    {
                        ScoredItem found = this.Sink.Find(this.CurrentContainer, image.Url);
                        if (found != null)
                        {
                            returnFile.Classifications = found.Classifications;
                        }
                    }

                    if (returnFile.Classifications.Count == 0)
                    {
                        returnFile.Classifications.Add(this.CurrentContainerAsClassification);
                        this.UpdateSourceFile(returnFile);
                    }

                    returnFiles.Add(returnFile);
                }
            }
            return returnFiles;
        }
        public IEnumerable<SourceFile> PreviousSourceGroup()
        {
            List<SourceFile> returnFiles = new List<SourceFile>();
            if (this.CanMovePrevious)
            {
                this.CurrentImage -= ((2 * this.BatchSize) + 1);
                returnFiles.AddRange(this.NextSourceGroup());
            }
            return returnFiles;

        }

        public void UpdateSourceBatch(IEnumerable<SourceFile> fileBatch)
        {
            if (this.Sink != null)
            {
                List<ScoredItem> updateList = new List<ScoredItem>();

                foreach (SourceFile file in fileBatch)
                {
                    ScoringImage image = this.CurrentImageList.FirstOrDefault(x => String.Compare(System.IO.Path.GetFileName(x.Url), file.Name, true) == 0);
                    if (image != null && this.Sink != null)
                    {
                        ScoredItem item = new ScoredItem()
                        {
                            Container = this.CurrentContainer,
                            Name = image.Url,
                            Classifications = file.Classifications
                        };

                        updateList.Add(item);
                    }
                }

                this.Sink.Record(updateList);

            }
        }

        #endregion

        #region IDataSource abstract overrides
        public override void ClearSourceFiles()
        {
            if(this.DeleteSourceFilesWhenComplete)
            {
                String downloadDirectory = System.IO.Path.Combine(this.Configuration.StorageConfiguration.RecordLocation, "temp");
                FileUtils.DeleteFiles(downloadDirectory, new string[] { this.Configuration.StorageConfiguration.FileType });
            }
        }

        public override IEnumerable<string> Containers { get { return this.PersistenceLogger.LabelMap.Keys; } }

        public override void SetContainer(string container)
        {
            if (this.Containers.Contains(container) &&
                String.Compare(this.CurrentContainer, container) != 0)
            {
                this.CurrentContainer = container;
                this.InitializeOnNewContainer();
            }
        }

        public override int CurrentContainerIndex { get { return this.CurrentImage; } }

        public override int CurrentContainerCollectionCount { get { return this.CurrentImageList.Count(); } }

        public override IEnumerable<string> CurrentContainerCollectionNames
        {
            get
            {
                List<string> itemNames = new List<string>();
                foreach (ScoringImage item in this.CurrentImageList)
                {
                    itemNames.Add(item.Url);
                }
                return itemNames;
            }
        }

        public override bool CanMoveNext
        {
            get
            {
                return !(this.CurrentImage >= this.CurrentImageList.Count - 1);
            }
        }

        public override bool CanMovePrevious
        {
            get
            {
                return !((this.CurrentImage - this.BatchSize) < 0);
            }
        }

        public override bool JumpToSourceFile(int index)
        {
            bool returnValue = true;
            String error = String.Empty;

            if (this.CurrentImageList == null || this.CurrentImageList.Count == 0)
            {
                error = "A colleciton must be present to use the Jump To function.";
            }
            else if (index > this.CurrentImageList.Count || index < 1)
            {
                error = String.Format("Jump to index must be within the collection size :: 1-{0}", this.CurrentImageList.Count);
            }
            else
            {
                this.CurrentImage = index - 2; // Have to move past the one before because next increments by 1
            }

            if (!String.IsNullOrEmpty(error))
            {
                System.Windows.MessageBox.Show(error, "Jump To Error", MessageBoxButton.OK, MessageBoxImage.Error);
                returnValue = false;
            }

            return returnValue;
        }

        public override void UpdateSourceFile(SourceFile file)
        {
            ScoringImage image = this.CurrentImageList.FirstOrDefault(x => String.Compare(System.IO.Path.GetFileName(x.Url), file.Name, true) == 0);
            if (image != null && this.Sink != null)
            {
                ScoredItem item = new ScoredItem()
                {
                    Container = this.CurrentContainer,
                    Name = image.Url,
                    Classifications = file.Classifications
                };
                this.Sink.Record(item);
            }
        }
        #endregion

        #region Private Helpers

        private void InitializeOnNewContainer()
        {
            this.CurrentImage = -1;
            this.CurrentImageList = this.PersistenceLogger.LoadContainerData(this.CurrentContainer);
        }

        /// <summary>
        /// Triggered by the configuration UI when the underlying configuration is updated.
        /// 
        /// Performs work then bubbles the call out to the parent app (listener)
        /// </summary>
        /// <param name="caller">unused</param>
        private void ConfigurationSaved(object caller)
        {
            // Save the configuration
            this.SaveConfiguration(this.Configuration);
            // Update the storage utils
            this.AzureStorageUtils = new StorageUtility(this.Configuration.StorageConfiguration);
            // Notify anyone who wants to be notified
            this.ConfigurationControl.OnConfigurationUdpated?.Invoke(this);
            this.OnLabelsAcquired?.Invoke(this.GetContainerLabels());

        }

        public string CurrentContainerAsClassification
        {
            get { return this.CleanContainerForClassification(this.CurrentContainer); }
        }

        private String CleanContainerForClassification(string container)
        {
            string returnValue = String.Empty;
            string cont = container.Trim(new char[] { '/' });

            int idx = cont.LastIndexOf('/');
            if (idx > 0)
            {
                returnValue = cont.Substring(idx + 1);
            }
            else
            {
                returnValue = cont;
            }
            return returnValue;
        }
        public IEnumerable<string> GetContainerLabels()
        {
            List<string> returnLabels = new List<string>();
            foreach(String container in this.Containers)
            {
                returnLabels.Add(this.CleanContainerForClassification(container));
            }
            return returnLabels;
        }

        private void AcquireContent(object caller)
        {
            // Update the data source
            if (System.Windows.MessageBox.Show(
                String.Format("This action will delete all files in : {0}{1}Further, the configuation will be updated.{1}Would you like to continue?",
                    this.Configuration.StorageConfiguration.RecordLocation,
                    Environment.NewLine),
                "Acquire Storage File List",
                MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            {
                return;
            }

            // Delete the ISink storage
            if (this.Sink != null)
            {
                this.Sink.Purge();
            }

            // add in the window to let them know we're working, see AzureBlobSource:260
            AcquireContentWindow contentWindow = new AcquireContentWindow();
            contentWindow.DisplayContent = String.Format("Acquiring {0} files from {1}", /*this.Configuration.FileCount*/ "all", this.Configuration.StorageConfiguration.StorageAccount);
            if (this.ConfigurationControl.Parent != null)
            {
                contentWindow.Top = this.ConfigurationControl.Parent.Top + (this.ConfigurationControl.Parent.Height - contentWindow.Height) / 2;
                contentWindow.Left = this.ConfigurationControl.Parent.Left + (this.ConfigurationControl.Parent.Width - contentWindow.Width) / 2;
            }
            contentWindow.Show();
             
            // Clean up current catalog data and reget the persistence logger
            FileUtils.DeleteFiles(this.Configuration.StorageConfiguration.RecordLocation, new string[] { "*.csv" });
            this.PersistenceLogger = new LabelledBlobPersisteceLogger(this.Configuration.StorageConfiguration);

            List<String> directories = new List<string>();
            foreach (String dir in this.AzureStorageUtils.ListDirectories(this.Configuration.StorageConfiguration.StorageContainer, this.Configuration.StorageConfiguration.BlobPrefix, false))
            {
                directories.Add(dir);
            }

            try
            {
                foreach (string directory in directories)
                {
                    foreach (KeyValuePair<string, string> kvp in this.AzureStorageUtils.ListBlobs(this.Configuration.StorageConfiguration.StorageContainer,
                        directory,
                        this.Configuration.StorageConfiguration.FileType,
                        false))
                    {
                        this.PersistenceLogger.RecordLabelledImage(directory, kvp.Value);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Azure Storage Error");
            }

            // Close window saying we are downloading 
            contentWindow.Close();


            // Update class variables
            this.CurrentContainer = this.Containers.FirstOrDefault();

            // Get the new data
            this.InitializeOnNewContainer();

            this.OnLabelsAcquired?.Invoke(this.GetContainerLabels());
        }

        private String DownloadStorageFile(string imageUrl)
        {
            String downloadDirectory = System.IO.Path.Combine(this.Configuration.StorageConfiguration.RecordLocation, "temp");
            FileUtils.EnsureDirectoryExists(downloadDirectory);

            string downloadFile = System.IO.Path.Combine(downloadDirectory, String.Format("{0}.jpg", (Guid.NewGuid().ToString("N"))));

            this.AzureStorageUtils.DownloadBlob(imageUrl, downloadFile);

            return downloadFile;
        }

        #endregion

    }
}
