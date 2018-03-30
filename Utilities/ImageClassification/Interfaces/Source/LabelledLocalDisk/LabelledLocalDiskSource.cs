using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.GlobalUtils;
using ImageClassifier.Interfaces.GlobalUtils.AzureStorage;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using ImageClassifier.Interfaces.Source.LabelledLocalDisk.Configuration;
using ImageClassifier.Interfaces.Source.LabelledLocalDisk.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ImageClassifier.Interfaces.Source.LabelledLocalDisk
{
    class LabelledLocalDiskSource : DataSourceBase<LabelledLocalConfiguration, String>, IMultiImageDataSource
    {
        #region Private Members
        /// <summary>
        /// Configuration file for this source
        /// </summary>
        private LabelledLocalConfiguration Configuration { get; set; }
        /// <summary>
        /// List of directories, which in this case become the containers.
        /// </summary>
        private List<String> DirectoryListings { get; set; }
        #endregion

        public LabelledLocalDiskSource()
        : base("LabelledLocalStorageConfiguration.json")
        {
            this.Name = "LabelledLocalStorageService";
            this.SourceType = DataSourceType.Disk;
            this.MultiClass = false;
            this.DeleteSourceFilesWhenComplete = false;

            // Get the configuration specific to this instance
            this.Configuration = this.LoadConfiguration();

            // Prepare the UI control with the right hooks.
            CutomLocalConfiguration configUi = new CutomLocalConfiguration(this, this.Configuration);
            configUi.OnConfigurationSaved += ConfigurationSaved;
            configUi.OnSourceDataUpdated += UpdateInformationRequested;


            this.ConfigurationControl = new ConfigurationControlImpl("Labelled Local Storage Service",
                configUi);

            this.UpdateInformationRequested(null);
            this.InitializeOnNewContainer();

            this.ContainerControl = new GenericContainerControl(this);
            this.ImageControl = new MultiImageControl(this);
        }

        #region IMultiImageDataSource
        public event OnContainerLabelsAcquired OnLabelsAcquired;
        public int BatchSize { get { return this.Configuration.BatchSize; } }

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
                    string image = this.CurrentImageList[++this.CurrentImage];

                    // Download blah blah

                    SourceFile returnFile = new SourceFile();
                    returnFile.Name = System.IO.Path.GetFileName(image);
                    returnFile.DiskLocation = image;

                    if (this.Sink != null)
                    {
                        ScoredItem found = this.Sink.Find(this.CurrentContainer, image);
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
                    string image = this.CurrentImageList.FirstOrDefault(x => String.Compare(x, file.Name, true) == 0);
                    if (image != null && this.Sink != null)
                    {
                        ScoredItem item = new ScoredItem()
                        {
                            Container = this.CurrentContainer,
                            Name = image,
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
            if (this.DeleteSourceFilesWhenComplete)
            {
                // We are not cleaning up anything at the moment
            }
        }
        public override IEnumerable<string> Containers { get { return this.DirectoryListings; } }
        public override int CurrentContainerIndex { get { return this.CurrentImage; } }
        public override int CurrentContainerCollectionCount { get { return this.CurrentImageList.Count(); } }
        public override IEnumerable<string> CurrentContainerCollectionNames
        {
            get
            {
                List<string> itemNames = new List<string>();
                foreach (string item in this.CurrentImageList)
                {
                    itemNames.Add(item);
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
                return !(this.CurrentImage <= 0);
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
            else if ((index-1) > this.CurrentImageList.Count || index < 1)
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
        public override void SetContainer(string container)
        {
            if (this.DirectoryListings.Contains(container) &&
                String.Compare(this.CurrentContainer, container) != 0)
            {
                this.CurrentContainer = container;
                this.InitializeOnNewContainer();
            }
        }

        public override void UpdateSourceFile(SourceFile file)
        {
            //throw new NotImplementedException();
            if (this.Sink != null)
            {
                ScoredItem item = new ScoredItem()
                {
                    Container = this.CurrentContainer,
                    Name = file.DiskLocation,
                    Classifications = file.Classifications
                };
                this.Sink.Record(item);
            }

        }
        #endregion

        #region Private Helpers
        private void ConfigurationSaved(object caller)
        {
            // Save the configuration
            this.SaveConfiguration(this.Configuration);
            this.UpdateInformationRequested(this);
            this.CurrentImage = -1;
            // Notify anyone who wants to be notified
            this.ConfigurationControl.OnConfigurationUdpated?.Invoke(this);
            // Since there is no new acquisition of data, go and do this too
            this.ConfigurationControl.OnSourceDataUpdated?.Invoke(this);
        }

        private void UpdateInformationRequested(object caller)
        {
            // Update class variables
            this.DirectoryListings = new List<string>();

            // Collect all directories in the configuration
            // TODO: When doing this for multi image it's location,false,1
            this.DirectoryListings.AddRange(FileUtils.GetDirectoryHierarchy(this.Configuration.LocalConfiguration.RecordLocation, false, 1));
            this.CurrentContainer = this.DirectoryListings.FirstOrDefault();

            // Initialize the list of items
            this.InitializeOnNewContainer();

            // Notify listeners it just happened.
            this.ConfigurationControl.OnSourceDataUpdated?.Invoke(this);
        }


        private void InitializeOnNewContainer()
        {
            this.CurrentImage = -1;
            this.CurrentImageList = new List<String>();

            if (System.IO.Directory.Exists(this.CurrentContainer))
            {
                foreach (String file in ImageClassifier.Interfaces.GlobalUtils.FileUtils.ListFile(this.CurrentContainer, this.Configuration.LocalConfiguration.FileTypes))
                {
                    this.CurrentImageList.Add(file);
                }
            }
        }
        #endregion

        #region Private Helpers
        public string CurrentContainerAsClassification
        {
            get { return this.CleanContainerForClassification(this.CurrentContainer); }
        }

        private String CleanContainerForClassification(string container)
        {
            string returnValue = String.Empty;
            string cont = container.Trim(new char[] { '\\' });

            int idx = cont.LastIndexOf('\\');
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
        #endregion
    }

}
