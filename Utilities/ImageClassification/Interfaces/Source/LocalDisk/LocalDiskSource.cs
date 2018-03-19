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

using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.GlobalUtils;
using ImageClassifier.Interfaces.Source.LocalDisk.Configuration;
using ImageClassifier.Interfaces.Source.LocalDisk.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ImageClassifier.Interfaces.Source.LocalDisk
{
    /// <summary>
    /// An IDataSource implementation for local disk.
    /// </summary>
    class LocalDiskSource : ConfigurationBase<LocalDiskSourceConfiguration>, IDataSource
    {
        #region Private Members
        /// <summary>
        /// Configuration file for this source
        /// </summary>
        private LocalDiskSourceConfiguration Configuration { get; set; }
        /// <summary>
        /// Index into CurrentImageList
        /// </summary>
        private int CurrentImage { get; set; }
        /// <summary>
        /// List of files from the currently selected directory
        /// </summary>
        private List<String> CurrentImageList { get; set; }
        /// <summary>
        /// List of directories, which in this case become the containers.
        /// </summary>
        private List<String> DirectoryListings { get; set; }
        #endregion

        public LocalDiskSource()
        : base("LocalStorageConfiguration.json")
        {
            this.Name = "LocalStorageService";
            this.SourceType = DataSourceType.Disk;

            // Get the configuration specific to this instance
            this.Configuration = this.LoadConfiguration();

            // Prepare the UI control with the right hooks.
            LocalSourceConfigurationUi configUi = new LocalSourceConfigurationUi(this, this.Configuration);
            configUi.OnConfigurationSaved += ConfigurationSaved;
            configUi.OnSourceDataUpdated += UpdateInformationRequested;

            this.ConfigurationControl = new ConfigurationControlImpl("Local Storage Service",
                configUi);

            this.UpdateInformationRequested(null);
            this.InitializeOnNewContainer();

            this.ContainerControl = new GenericContainerControl(this);
            this.ImageControl = new SingleImageControl(this);
        }

        #region IDataSource

        #region General
        public string Name { get; private set; }
        public DataSourceType SourceType { get; private set; }
        public bool MultiClass { get { return false; } }
        public bool DeleteSourceFilesWhenComplete { get { return false; } }
        #endregion

        #region Interfaces
        public IDataSink Sink { get; set; }
        public IConfigurationControl ConfigurationControl { get; private set; }
        public IContainerControl ContainerControl { get; private set; }
        public IImageControl ImageControl { get; set; }
        #endregion

        public string CurrentContainer { get; private set; }
        public IEnumerable<string> Containers { get { return this.DirectoryListings; } }
        public int CurrentContainerIndex { get { return this.CurrentImage; } }
        public int CurrentContainerCollectionCount { get { return this.CurrentImageList.Count(); } }

        public IEnumerable<string> CurrentContainerCollectionNames
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
        public bool CanMoveNext
        {
            get
            {
                return !(this.CurrentImage >= this.CurrentImageList.Count - 1);
            }
        }
        public bool CanMovePrevious
        {
            get
            {
                return !(this.CurrentImage <= 0);
            }
        }
        public bool JumpToSourceFile(int index)
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
        public SourceFile NextSourceFile()
        {
            SourceFile returnFile = null;
            if (this.CanMoveNext)
            {
                if (this.CurrentImage <= -1)
                {
                    this.CurrentImage = -1;
                }

                string file = this.CurrentImageList[++this.CurrentImage];

                returnFile = new SourceFile();
                returnFile.Name = System.IO.Path.GetFileName(file);
                returnFile.Classifications = new List<String>();
                returnFile.DiskLocation = file;

                // Is it cataloged?
                if(this.Sink != null)
                {
                    ScoredItem found = this.Sink.Find(this.CurrentContainer, returnFile.DiskLocation);
                    if(found != null)
                    {
                        returnFile.Classifications = found.Classifications;
                    }
                }


            }
            return returnFile;
        }
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

        public void SetContainer(string container)
        {
            if (this.DirectoryListings.Contains(container) &&
                String.Compare(this.CurrentContainer, container) != 0)
            {
                this.CurrentContainer = container;
                this.InitializeOnNewContainer();
            }
        }

        public void UpdateSourceFile(SourceFile file)
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
            this.RecurseDirectoryListings(this.Configuration.RecordLocation);
            this.CurrentContainer = this.DirectoryListings.FirstOrDefault();

            // Initialize the list of items
            this.InitializeOnNewContainer();

            // Notify listeners it just happened.
            this.ConfigurationControl.OnSourceDataUpdated?.Invoke(this);
        }

        private void RecurseDirectoryListings(string baseDirectory)
        {
            if (System.IO.Directory.Exists(baseDirectory))
            {
                //String listing = String.IsNullOrEmpty(parent) ? baseDirectory : System.IO.Path.Combine(parent, baseDirectory);
                this.DirectoryListings.Add(baseDirectory);

                foreach (string directory in System.IO.Directory.GetDirectories(baseDirectory))
                {
                    this.RecurseDirectoryListings(directory);
                }
            }
        }

        private void InitializeOnNewContainer()
        {
            this.CurrentImage = -1;
            this.CurrentImageList = new List<String>();

            if (System.IO.Directory.Exists(this.CurrentContainer))
            {
                foreach (String file in ImageClassifier.Interfaces.GlobalUtils.FileUtils.ListFile(this.CurrentContainer, this.Configuration.FileTypes))
                {
                    this.CurrentImageList.Add(file);
                }
            }
        }
        #endregion

    }
}
