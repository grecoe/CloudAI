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
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.Source.LabeldBlobSource.Persistence;
using ImageClassifier.Interfaces.Source.LabeldBlobSource.Configuration;
using ImageClassifier.Interfaces.Source.LabeldBlobSource.UI;
using ImageClassifier.Interfaces.GlobalUtils.Persistence;
using ImageClassifier.Interfaces.GlobalUtils.AzureStorage;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;

namespace ImageClassifier.Interfaces.Source.LabeldBlobSource
{
    class LabeledAzureBlobSource : DataSourceBase<LabelledBlobSourceConfiguration, ScoringImage>, IMultiImageDataSource 
    {
        #region PrivateMembers
        private const int DEFAULT_BATCH_SIZE = 6;

        /// <summary>
        /// Custom Configuration that includes additional settings over and above the AzureStorageConfiguration
        /// </summary>
        private LabelledBlobSourceConfiguration Configuration { get; set; }
        /// <summary>
        /// The Azure Storage Account utility class
        /// </summary>
        private StorageUtility AzureStorageUtils { get; set; }
        /// <summary>
        /// Persists storage account information
        /// </summary>
        private LabelledBlobPersisteceLogger PersistenceLogger { get; set; }
        #endregion

        public LabeledAzureBlobSource()
            : base("LabeledAzureStorageConfiguration.json")
        {
            this.Name = "LabeledAzureStorage";
            this.SourceType = DataSourceType.LabelledBlob;
            this.DeleteSourceFilesWhenComplete = true;
            this.CurrentImage = -1;


            // Get the configuration specific to this instance
            this.Configuration = this.LoadConfiguration();
            this.MultiClass = this.Configuration.StorageConfiguration.MultiClass;

            if (this.Configuration.BatchSize <= 0 || this.Configuration.BatchSize > 9)
            {
                this.Configuration.BatchSize = LabeledAzureBlobSource.DEFAULT_BATCH_SIZE;
                this.SaveConfiguration(this.Configuration);
            }

            // Create the storage utils
            this.AzureStorageUtils = new StorageUtility(this.Configuration.StorageConfiguration);

            // Prepare the UI control with the right hooks.
            CustomStorageConfiguration configUi = new CustomStorageConfiguration(this, this.Configuration);
            configUi.OnConfigurationSaved += ConfigurationSaved;
            configUi.OnSourceDataUpdated += UpdateInformationRequested;

            this.ConfigurationControl =
                new ConfigurationControlImpl("Azure Storage - Labeled Dataset",
                configUi);

            // Get a list of containers through the persistence logger 
            if (!String.IsNullOrEmpty(this.Configuration.StorageConfiguration.StorageAccount) &&
                !String.IsNullOrEmpty(this.Configuration.StorageConfiguration.StorageAccountKey))
            {
                this.PersistenceLogger = new LabelledBlobPersisteceLogger(this.Configuration.StorageConfiguration);
                this.CurrentContainer = this.Containers.FirstOrDefault();
                this.InitializeOnNewContainer();
            }

            this.ContainerControl = new GenericContainerControl(this);
            this.ImageControl = new MultiImageControl(this);
        }

        #region IMultiImageDataSource

        public event OnContainerLabelsAcquired OnLabelsAcquired;
        public int BatchSize { get { return this.Configuration.BatchSize; } }

        public string CurrentContainerAsClassification
        {
            get { return this.CleanContainerForClassification(this.CurrentContainer); }
        }

        public IEnumerable<string> GetContainerLabels()
        {
            List<string> returnLabels = new List<string>();
            foreach (String container in this.Containers)
            {
                returnLabels.Add(this.CleanContainerForClassification(container));
            }
            return returnLabels;
        }

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

                    // Was this.DownloadStorageFile(imageUrl)
                    returnFile.DiskLocation = this.AzureStorageUtils.DownloadImageBlob(
                        imageUrl,
                        System.IO.Path.Combine(this.Configuration.StorageConfiguration.RecordLocation, "temp"));

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
            if (this.Sink != null && !String.IsNullOrEmpty(this.CurrentContainer))
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

        #region IDataSource Virtual Overrides
        public override void ClearSourceFiles()
        {
            if (this.DeleteSourceFilesWhenComplete && !String.IsNullOrEmpty(this.Configuration.StorageConfiguration.RecordLocation))
            {
                String downloadDirectory = System.IO.Path.Combine(this.Configuration.StorageConfiguration.RecordLocation, "temp");
                FileUtils.DeleteFiles(downloadDirectory, new string[] { this.Configuration.StorageConfiguration.FileType });
            }
        }
        #endregion

        #region IDataSource abstract Overrides
        public override IEnumerable<string> Classifications { get { return this.Configuration.StorageConfiguration.Classifications; } }

        public override IEnumerable<string> Containers
        {
            get {
                if (this.PersistenceLogger != null)
                {
                    return this.PersistenceLogger.LabelMap.Keys;
                }

                return new List<string>();
            }
        }

        public override void SetContainer(string container)
        {
            if (this.Containers.Contains(container) &&
                String.Compare(this.CurrentContainer, container) != 0)
            {
                this.CurrentContainer = container;
                this.InitializeOnNewContainer();
            }
        }

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

        /// <summary>
        /// When a new container is selected, reset the interal list and list index
        /// </summary>
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

            // Update Multiclass
            this.MultiClass = this.Configuration.StorageConfiguration.MultiClass;
            
            // Update the storage utils - may have new settings
            this.AzureStorageUtils = new StorageUtility(this.Configuration.StorageConfiguration);

            // Clean the labels for the UI
            this.OnLabelsAcquired?.Invoke(this.GetContainerLabels());

            // Update containers
            this.ContainerControl.Refresh();

            // Notify anyone who wants to be notified
            this.ConfigurationControl.OnConfigurationUdpated?.Invoke(this);
        }

        /// <summary>
        /// Does the actual cleaning of a container name by stripping the last directory out
        /// of the path. In this store that is the base classification of any item.
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        private String CleanContainerForClassification(string container)
        {
            string returnValue = String.Empty;
            if (!String.IsNullOrEmpty(container))
            {
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
            }
            return returnValue;
        }

        /// <summary>
        /// Call to load data from the storage account. This call will delete all other data that has been acquired, downloaded, or scored locally. 
        /// 
        /// After cleaning re-builds local catalogs with new data.
        /// </summary>
        private void UpdateInformationRequested(object caller)
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
            AcquireContentWindow contentWindow = new AcquireContentWindow(this.ConfigurationControl.Parent);
            int totalDownloadCount = (this.Configuration.StorageConfiguration.FileCount == StorageUtility.DEFAULT_FILE_COUNT) ? StorageUtility.DEFAULT_DOWNLOAD_COUNT : this.Configuration.StorageConfiguration.FileCount;

            contentWindow.DisplayContent = String.Format("Acquiring (max) {0} files from {1}{2}This may take a long time, please bear with us.....", 
                totalDownloadCount, 
                this.Configuration.StorageConfiguration.StorageAccount,
                Environment.NewLine);
            contentWindow.Show();
             
            // Clean up current catalog data and reget the persistence logger
            FileUtils.DeleteFiles(this.Configuration.StorageConfiguration.RecordLocation, new string[] { "*.csv" });
            this.PersistenceLogger = new LabelledBlobPersisteceLogger(this.Configuration.StorageConfiguration);

            List<String> directories = new List<string>();
            try
            {
                foreach (String dir in this.AzureStorageUtils.ListDirectories(this.Configuration.StorageConfiguration.StorageContainer, this.Configuration.StorageConfiguration.BlobPrefix, false))
                {
                    directories.Add(dir);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Azure Storage Exception");
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
                        this.PersistenceLogger.RecordStorageImage(directory, kvp.Value);
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

            // Update containers
            this.ContainerControl?.Refresh();

            // Clean labels
            this.OnLabelsAcquired?.Invoke(this.GetContainerLabels());

            // Get the new data
            this.InitializeOnNewContainer();

            // Notify listeners it just happened.
            this.ConfigurationControl.OnSourceDataUpdated?.Invoke(this);

            // Reset the grid
            if (this.ImageControl is IMultiImageControl)
            {
                (this.ImageControl as IMultiImageControl).ResetGrid();
            }
        }
        #endregion
    }
}
