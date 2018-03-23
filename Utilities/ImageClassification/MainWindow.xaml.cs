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

using ImageClassifier.Configuration;
using ImageClassifier.Interfaces;
using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.Source.LabeldBlobSource;
using ImageClassifier.UIUtils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ImageClassifier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Properties
        /// <summary>
        /// Configuration settings from Classification.json
        /// </summary>
        private ClassificationContext ConfigurationContext { get; set; }
        /// <summary>
        /// The current data source provider
        /// </summary>
        private IDataSource SelectedDataSource { get; set; }
        /// <summary>
        /// A list of all known data sources
        /// </summary>
        private List<IDataSource> DataSources { get; set; }
        #endregion


        public MainWindow()
        {
            InitializeComponent();

            // Class Objects
            this.ConfigurationContext = ClassificationContext.LoadConfiguration();

            // Hook events for the status bar
            this.StatusButtonJumpTo.Click += (o, e) => StatusBarJumpToImage();

            // Source Provider setup and hooking the provider combo change event 
            this.LoadSourceProviders();
            this.ConfigurationTabSourceProviderCombo.SelectionChanged += (o, e) => SourceProviderChanged();

            // Now make sure we have the correct selection for the provider based on
            // the default, which is set every time the user changes it, if it's not
            // set just set the first one, but it has to happen here to get the right
            // context for the UI before initializing
            if (!String.IsNullOrEmpty(this.ConfigurationContext.DefaultProvider))
            {
                foreach(object child in this.ConfigurationTabSourceProviderCombo.Items)
                {
                    DataSourceComboItem comboItem = child as DataSourceComboItem;
                    if (comboItem != null && String.Compare(comboItem.Source.Name, this.ConfigurationContext.DefaultProvider) == 0)
                    {
                        this.ConfigurationTabSourceProviderCombo.SelectedIndex =
                            this.ConfigurationTabSourceProviderCombo.Items.IndexOf(child);
                        break;
                    }
                }
            }
            else
            {
                this.ConfigurationTabSourceProviderCombo.SelectedIndex = 0;
            }

            // Hook the closing event so we can ensure we capture all changes
            this.Closing += WindowClosing;

            // Initialize with the settings we have.
            InitializeUi(true);
        }

        /// <summary>
        /// Load in all of the source providers. Currently does not allow extensions
        /// or third party providers, which is a pretty simple addition.
        /// 
        /// Right now, it's an azure blob source or local file source
        /// </summary>
        private void LoadSourceProviders()
        {
            // Blob Source
            IDataSource blobSource = DataSourceFactory.GetDataSource(DataSourceProvider.AzureBlob,DataSink.Catalog);
            blobSource.ConfigurationControl.OnConfigurationUdpated = this.IDataSourceOnConfigurationUdpated;
            blobSource.ConfigurationControl.OnSourceDataUpdated = this.IDataSourceOnSourceDataUpdated;
            blobSource.ConfigurationControl.Parent = this;

            TabItem tabItem = new TabItem();
            tabItem.Header = blobSource.ConfigurationControl.Title;
            tabItem.Content = blobSource.ConfigurationControl.Control;
            this.ConfigurationTabSourceTabControl.Items.Add(tabItem);

            // Disk Source
            IDataSource localSource = DataSourceFactory.GetDataSource(DataSourceProvider.LocalDisk, DataSink.Catalog);
            localSource.ConfigurationControl.OnConfigurationUdpated = this.IDataSourceOnConfigurationUdpated;
            localSource.ConfigurationControl.OnSourceDataUpdated = this.IDataSourceOnSourceDataUpdated;
            localSource.ConfigurationControl.Parent = this;

            tabItem = new TabItem();
            tabItem.Header = localSource.ConfigurationControl.Title;
            tabItem.Content = localSource.ConfigurationControl.Control;
            this.ConfigurationTabSourceTabControl.Items.Add(tabItem);

            // Labelled blob Source
            IDataSource labelledBlobSource = DataSourceFactory.GetDataSource(DataSourceProvider.LabeledAzureBlob, DataSink.Catalog);
            labelledBlobSource.ConfigurationControl.OnConfigurationUdpated = this.IDataSourceOnConfigurationUdpated;
            labelledBlobSource.ConfigurationControl.OnSourceDataUpdated = this.IDataSourceOnSourceDataUpdated;
            labelledBlobSource.ConfigurationControl.Parent = this;

            tabItem = new TabItem();
            tabItem.Header = labelledBlobSource.ConfigurationControl.Title;
            tabItem.Content = labelledBlobSource.ConfigurationControl.Control;
            this.ConfigurationTabSourceTabControl.Items.Add(tabItem);

            this.DataSources = new List<IDataSource>() { blobSource, localSource, labelledBlobSource };

            // Set the UI up but don't select one, happens later after we hook the selection changed.
            foreach (IDataSource source in this.DataSources)
            {
                this.ConfigurationTabSourceProviderCombo.Items.Add(new DataSourceComboItem(source));
            }
        }

        /// <summary>
        /// Capture the window closing event and force save out the current image as it may 
        /// have changed.
        /// </summary>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.ForceClassificationUpdate();
        }

        /// <summary>
        /// Helper function used whenever we need to save the current selections
        /// </summary>
        private void ForceClassificationUpdate()
        {
            if (this.SelectedDataSource != null && this.SelectedDataSource.ImageControl != null)
            {
                this.SelectedDataSource.ImageControl.UpdateClassifications(
                    ClassificationCheckboxPanelHelper.CollectSelections(this.ClassificationTabSelectionPanel)
                    );
            }
        }
    }
}
