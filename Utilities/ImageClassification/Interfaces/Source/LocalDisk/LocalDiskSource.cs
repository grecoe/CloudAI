﻿//
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
using System.Linq;
using System.Windows;
using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using ImageClassifier.Interfaces.GlobalUtils.Persistence;

namespace ImageClassifier.Interfaces.Source.LocalDisk
{
    /// <summary>
    /// An IDataSource implementation for local disk.
    /// </summary>
    class LocalDiskSource : DataSourceBase<LocalDiskSourceConfiguration, String>, ISingleImageDataSource 
    {
        #region Private Members
        /// <summary>
        /// Configuration file for this source
        /// </summary>
        private LocalDiskSourceConfiguration Configuration { get; set; }
        /// <summary>
        /// List of directories, which in this case become the containers.
        /// </summary>
        private List<String> DirectoryListings { get; set; }
        #endregion

        public LocalDiskSource()
        : base("LocalMachineConfiguration.json")
        {
            this.Name = "LocalMachine";
            this.SourceType = DataSourceType.Disk;
            this.DeleteSourceFilesWhenComplete = false;

            // Get the configuration specific to this instance
            this.Configuration = this.LoadConfiguration();
            this.MultiClass = this.Configuration.MultiClass;


            // Prepare the UI control with the right hooks.
            LocalSourceConfigurationUi configUi = new LocalSourceConfigurationUi(this, this.Configuration);
            configUi.OnConfigurationSaved += ConfigurationSaved;
            configUi.OnSourceDataUpdated += UpdateInformationRequested;

            this.ConfigurationControl = new ConfigurationControlImpl("Local Machine - Unlabeled Dataset",
                configUi);

            this.UpdateInformationRequested(null);
            this.InitializeOnNewContainer();

            this.ContainerControl = new GenericContainerControl(this);
            this.ImageControl = new SingleImageControl(this);
        }

        #region ISingleImageDataSource
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
                if (this.Sink != null)
                {
                    ScoredItem found = this.Sink.Find(this.CurrentContainer, returnFile.DiskLocation);
                    if (found != null)
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
        #endregion

        #region IDataSource abstract overrides
        public override IEnumerable<string> Classifications { get { return this.Configuration.Classifications; } }

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
        /// Triggered by the configuration UI when the user clicks save configuration.
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

            // Update multiclass
            this.MultiClass = this.Configuration.MultiClass;

            // Notify anyone who wants to be notified
            this.ConfigurationControl.OnConfigurationUdpated?.Invoke(this);
            
            // Since there is no new acquisition of data, go and do this too
            // This will trigger the UpdateInformationRequested()
            this.ConfigurationControl.OnSourceDataUpdated?.Invoke(this);
        }

        /// <summary>
        /// Triggered by the configuraiton UI, during initialization and during configuration changes being saved
        /// to update the internal data.
        /// </summary>
        private void UpdateInformationRequested(object caller)
        {
            // Update class variables
            this.DirectoryListings = new List<string>();

            // Collect all directories in the configuration
            this.DirectoryListings.AddRange(FileUtils.GetDirectoryHierarchy(this.Configuration.RecordLocation, true, 0));
            this.CurrentContainer = this.DirectoryListings.FirstOrDefault();
            
            // Initialize the list of items
            this.InitializeOnNewContainer();

            // Update containers
            this.ContainerControl?.Refresh();

            // Notify listeners it just happened.
            this.ConfigurationControl.OnSourceDataUpdated?.Invoke(this);
        }

        /// <summary>
        /// Update the internal image list and index when a new container is selected.
        /// </summary>
        private void InitializeOnNewContainer()
        {
            this.CurrentImage = -1;
            this.CurrentImageList = new List<String>();

            if (System.IO.Directory.Exists(this.CurrentContainer))
            {
                foreach (String file in FileUtils.ListFile(this.CurrentContainer, this.Configuration.FileTypes))
                {
                    this.CurrentImageList.Add(file);
                }
            }
        }
        #endregion

    }
}
