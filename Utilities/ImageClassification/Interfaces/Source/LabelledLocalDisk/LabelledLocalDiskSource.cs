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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using ImageClassifier.Interfaces.Source.LabelledLocalDisk.Configuration;
using ImageClassifier.Interfaces.Source.LabelledLocalDisk.UI;
using ImageClassifier.Interfaces.GlobalUtils.Persistence;

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
        : base("LabeledLocalMachineConfiguration.json")
        {
            this.Name = "LabeledLocalMachine";
            this.SourceType = DataSourceType.LabelledDisk;
            this.DeleteSourceFilesWhenComplete = false;

            // Get the configuration specific to this instance
            this.Configuration = this.LoadConfiguration();
            this.MultiClass = this.Configuration.LocalConfiguration.MultiClass;

            // Prepare the UI control with the right hooks.
            CutomLocalConfiguration configUi = new CutomLocalConfiguration(this, this.Configuration);
            configUi.OnConfigurationSaved += ConfigurationSaved;
            configUi.OnSourceDataUpdated += UpdateInformationRequested;


            this.ConfigurationControl = new ConfigurationControlImpl("Local Machine - Labeled Dataset",
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

        public string CurrentContainerAsClassification
        {
            get { return this.CleanContainerForClassification(this.CurrentContainer); }
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
            if (this.Sink != null && !String.IsNullOrEmpty(this.CurrentContainer) )
            {
                List<ScoredItem> updateList = new List<ScoredItem>();

                foreach (SourceFile file in fileBatch)
                {
                    string image = this.CurrentImageList.FirstOrDefault(x => String.Compare(x, file.DiskLocation, true) == 0);
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
        public override IEnumerable<string> Classifications { get { return this.Configuration.LocalConfiguration.Classifications; } }

        public override IEnumerable<string> Containers { get { return this.DirectoryListings; } }

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
        /// <summary>
        /// Performs the steps needed on saving the configuration and re-initializing
        /// </summary>
        private void ConfigurationSaved(object caller)
        {
            // Delete the ISink storage
            if (this.Sink != null)
            {
                this.Sink.Purge();
            }

            // Save the configuration
            this.SaveConfiguration(this.Configuration);
            this.CurrentImage = -1;

            // Update containers
            this.ContainerControl.Refresh();

            // Update multiclass
            this.MultiClass = this.Configuration.LocalConfiguration.MultiClass;

            // Notify anyone who wants to be notified
            this.ConfigurationControl.OnConfigurationUdpated?.Invoke(this);
        }

        /// <summary>
        /// When teh configuration is saved and new information needs to be loaded, re-initialized,
        /// and notify any listeners of the change.
        /// </summary>
        private void UpdateInformationRequested(object caller)
        {
            // Update class variales
            this.DirectoryListings = new List<string>();

            // Collect all directories in the configuration
            this.DirectoryListings.AddRange(FileUtils.GetDirectoryHierarchy(this.Configuration.LocalConfiguration.RecordLocation, false, 1));
            this.CurrentContainer = this.DirectoryListings.FirstOrDefault();

            // Update containers
            this.ContainerControl?.Refresh();

            // Initialize the list of items
            this.InitializeOnNewContainer();

            // Notify listeners it just happened.
            this.ConfigurationControl.OnSourceDataUpdated?.Invoke(this);

            // Update the grid
            if (this.ImageControl is IMultiImageControl)
            {
                (this.ImageControl as IMultiImageControl).ResetGrid();
            }
        }

        /// <summary>
        /// Resets the internal list and list index when a container is changed.
        /// </summary>
        private void InitializeOnNewContainer()
        {
            this.CurrentImage = -1;
            this.CurrentImageList = new List<String>();

            // If a directory was chosen that doens't have children there are no containers
            if (!String.IsNullOrEmpty(this.CurrentContainer))
            {
                if (System.IO.Directory.Exists(this.CurrentContainer))
                {
                    foreach (String file in FileUtils.ListFile(this.CurrentContainer, this.Configuration.LocalConfiguration.FileTypes))
                    {
                        this.CurrentImageList.Add(file);
                    }
                }
            }
        }

        /// <summary>
        /// In this source, we use the last directory in the path as the base classificaiton for 
        /// the item, so retrieve just the last directory in the data.
        /// </summary>
        private String CleanContainerForClassification(string container)
        {
            string returnValue = String.Empty;

            if (!String.IsNullOrEmpty(container))
            {
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
            }
            return returnValue;
        }
        #endregion
    }

}
