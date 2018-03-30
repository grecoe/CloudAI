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
using ImageClassifier.Interfaces.Source.BlobSource.Persistence;
using ImageClassifier.Interfaces.GlobalUtils;
using ImageClassifier.Interfaces.GlobalUtils.AzureStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;

namespace ImageClassifier.Interfaces.Source.BlobSource
{
    /// <summary>
    /// An IDataSource implementation for Azure Blob Storage.
    /// </summary>
    class AzureBlobSource : DataSourceBase<AzureBlobStorageConfiguration, ScoringImage>, ISingleImageDataSource 
    {
        #region PrivateMembers
        private AzureBlobStorageConfiguration Configuration { get; set; }

        private StorageUtility AzureStorageUtils { get; set; }

        private List<string> CatalogFiles { get; set; }
        #endregion

        public AzureBlobSource()
            :base("AzureStorageConfiguration.json")
        {
            this.Name = "AzureStorageSource";
            this.SourceType = DataSourceType.Blob;
            this.DeleteSourceFilesWhenComplete = true;
            this.MultiClass = true;

            this.CurrentImage = -1;

            // Get the configuration specific to this instance
            this.Configuration = this.LoadConfiguration();

            // Create the storage utils
            this.AzureStorageUtils = new StorageUtility(this.Configuration);

            // Prepare the UI control with the right hooks.
            AzureStorageConfigurationUi configUi = new AzureStorageConfigurationUi(this,this.Configuration);
            configUi.OnConfigurationSaved += ConfigurationSaved;
            configUi.OnSourceDataUpdated += UpdateInformationRequested;

            this.ConfigurationControl = 
                new ConfigurationControlImpl("Azure Storage Service",
                configUi);

            // Load the catalogs and initialize if already configured
            this.CatalogFiles = new List<string>(BlobPersistenceLogger.GetAcquisitionFiles(this.Configuration));
            this.CurrentContainer = this.CatalogFiles.FirstOrDefault();
            this.InitializeOnNewContainer();

            this.ContainerControl = new GenericContainerControl(this);
            this.ImageControl = new SingleImageControl(this);
        }

        #region ISingleImageSource
        public SourceFile PreviousSourceFile()
        {
            SourceFile returnFile = null;
            if (this.CanMovePrevious)
            {
                this.CurrentImage -= 2;
                returnFile = this.NextSourceFile();
            }
            return returnFile;
        }

        public SourceFile NextSourceFile()
        {
            SourceFile returnFile = null;
            if (this.CanMoveNext)
            {
                if (this.CurrentImage <= -1)
                {
                    this.CurrentImage = -1;
                }
                ScoringImage image = this.CurrentImageList[++this.CurrentImage];


                // Download blah blah
                String token = this.AzureStorageUtils.GetSasToken(this.Configuration.StorageContainer);
                String imageUrl = String.Format("{0}{1}", image.Url, token);

                returnFile = new SourceFile();
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
            }
            return returnFile;
        }
        #endregion

        #region IDataSource abstract overrides
        public override void ClearSourceFiles()
        {
            if (this.DeleteSourceFilesWhenComplete)
            {
                String downloadDirectory = System.IO.Path.Combine(this.Configuration.RecordLocation, "temp");
                FileUtils.DeleteFiles(downloadDirectory, new string[] { this.Configuration.FileType });
            }
        }
        public override IEnumerable<string> Containers { get { return this.CatalogFiles; } }
        public override void SetContainer(string container)
        {
            if (this.CatalogFiles.Contains(container) &&
                String.Compare(this.CurrentContainer, container) != 0)
            {
                this.CurrentContainer = container;
                this.InitializeOnNewContainer();
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
                return !(this.CurrentImage < 0);
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

        #region Support Methods
        /// <summary>
        /// Triggered by the configuration UI when the underlying source catalog is updated.
        /// 
        /// Performs work then bubbles the call out to the parent app (listener)
        /// </summary>
        /// <param name="caller">unused</param>
        private void UpdateInformationRequested(object caller)
        {
            // Update the data source
            if (System.Windows.MessageBox.Show(
                String.Format("This action will delete all files in : {0}{1}Further, the configuation will be updated.{1}Would you like to continue?",
                    this.Configuration.RecordLocation,
                    Environment.NewLine),
                "Acquire Storage File List",
                MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            {
                return;
            }

            // Delete the ISink storage
            if(this.Sink != null)
            {
                this.Sink.Purge();
            }

            // Create an overlay window so we can at least show something while we do work. 
            AcquireContentWindow contentWindow = new AcquireContentWindow();
            contentWindow.DisplayContent = String.Format("Acquiring {0} files from {1}", this.Configuration.FileCount, this.Configuration.StorageAccount);
            if (this.ConfigurationControl.Parent != null)
            {
                contentWindow.Top = this.ConfigurationControl.Parent.Top + (this.ConfigurationControl.Parent.Height - contentWindow.Height) / 2;
                contentWindow.Left = this.ConfigurationControl.Parent.Left + (this.ConfigurationControl.Parent.Width - contentWindow.Width) / 2;
            }
            contentWindow.Show();

            // Clean up current catalog data
            FileUtils.DeleteFiles(this.Configuration.RecordLocation, new string[] { "*.csv" });


            // Load data from storage
            int fileLabel = 1;
            int recordCount = 0;
            BlobPersistenceLogger logger = new BlobPersistenceLogger(this.Configuration, fileLabel);
            try
            {
                foreach (KeyValuePair<String, String> kvp in this.AzureStorageUtils.ListBlobs(false))
                {
                    logger.Record(new string[] { kvp.Value });

                    // Too many?
                    if (++recordCount >= this.Configuration.FileCount)
                    {
                        break;
                    }
                    // Have to roll the file?
                    if (recordCount % 100 == 0)
                    {
                        logger = new BlobPersistenceLogger(this.Configuration, ++fileLabel);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Azure Storage Exception");
            }

            contentWindow.Close();

            // Update class variables
            this.CatalogFiles = new List<string>(BlobPersistenceLogger.GetAcquisitionFiles(this.Configuration));
            this.CurrentContainer = this.CatalogFiles.FirstOrDefault();

            this.InitializeOnNewContainer();

            // Notify listeners it just happened.
            this.ConfigurationControl.OnSourceDataUpdated?.Invoke(this);
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
            this.AzureStorageUtils = new StorageUtility(this.Configuration);
            // Notify anyone who wants to be notified
            this.ConfigurationControl.OnConfigurationUdpated?.Invoke(this);
        }

        /// <summary>
        /// Called when the container has been changed. Data for the new container is loaded.
        /// </summary>
        private void InitializeOnNewContainer()
        {
            this.CurrentImage = -1;
            this.CurrentImageList = new List<ScoringImage>();

            if (!String.IsNullOrEmpty(this.Configuration.RecordLocation) && !String.IsNullOrEmpty(this.CurrentContainer))
            {
                String dataPath = System.IO.Path.Combine(this.Configuration.RecordLocation, this.CurrentContainer);

                if (System.IO.File.Exists(dataPath))
                {
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(dataPath))
                    {
                        bool header = false;
                        while (!reader.EndOfStream)
                        {
                            String content = reader.ReadLine();
                            if (!header)
                            {
                                header = true;
                                continue;
                            }

                            ScoringImage img = BlobPersistenceLogger.ParseRecord(content);
                            if (img != null)
                            {
                                this.CurrentImageList.Add(img);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Download an image from Azure Storage with the given URL. File is downloaded to a temp
        /// directory with a GUID name. 
        /// </summary>
        /// <param name="imageUrl">Azure Storage URL to the blob to be downloaded.</param>
        /// <returns>Local path location of the blob after download</returns>
        private String DownloadStorageFile(string imageUrl)
        {
            String downloadDirectory = System.IO.Path.Combine(this.Configuration.RecordLocation, "temp");
            FileUtils.EnsureDirectoryExists(downloadDirectory);

            string downloadFile = System.IO.Path.Combine(downloadDirectory, String.Format("{0}.jpg", (Guid.NewGuid().ToString("N"))));

            this.AzureStorageUtils.DownloadBlob(imageUrl, downloadFile);

            return downloadFile;
        }

        #endregion

    }
}
