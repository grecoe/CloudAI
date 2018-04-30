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
using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using ImageClassifier.Interfaces.GlobalUtils.Processing;
using System.Text;
using ImageClassifier.Interfaces.Source.LabeldBlobSource.Configuration;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// Control for collecting azure storage information.
    /// </summary>
    public partial class AzureStorageConfigurationUi : UserControl, IConfigurationControlNotifications
    {
        #region IConfigurationControlNotifications
        public event OnConfigurationUpdatedHandler OnConfigurationSaved;
        public event OnUpdateSourceData OnSourceDataUpdated;
        #endregion

        private List<ToggleButton> MultiClassSelections { get; set; }

        public IDataSource Provider { get; private set; }

        public AzureBlobStorageConfiguration Configuration { get; private set; }

        public AzureStorageConfigurationUi()
        {
            InitializeComponent();
        }

        public AzureStorageConfigurationUi(IDataSource source, AzureBlobStorageConfiguration config)
        {
            InitializeComponent();
            this.Provider = source;
            this.Configuration = config;
            this.MultiClassSelections = new List<ToggleButton>() { this.Negative, this.Affirmitive };

            this.ConfigurationButtonSave.Click += (o, e) => this.Collect();
            this.ConfigurationButtonAquireContent.Click += (o, e) => this.AcquireContent();
            this.ConfigurationButtonDirectory.Click += (o, e) => ConfigurationSelectLogFolder();


            this.Seed();

            // Hook Process buttons
            this.PreviewChangesButton.Click += (o, e) => PreviewChanges();
            this.ProcessChangesButton.Click += (o, e) => ProcessChanges();
            this.ModifyChangeButtons();
        }

        #region Processing
        private void ModifyChangeButtons()
        {
            bool enableChanges = !this.Configuration.MultiClass;
            this.PreviewChangesButton.IsEnabled = enableChanges;
            this.ProcessChangesButton.IsEnabled = enableChanges;
        }

        private void PreviewChanges()
        {
            if (this.Configuration != null)
            {
                SinkPostProcessStorage postProcess = new SinkPostProcessStorage(this.Provider.Sink, this.Configuration);
                if (!postProcess.ItemsToProcess)
                {
                    MessageBox.Show("There are no items to process at this time.", "Preview Changes", MessageBoxButton.OK);
                }
                else
                {
                    String status = postProcess.CollectSummary();
                    MessageBox.Show(status, "Process Changes Queued", MessageBoxButton.OK);
                }
            }
        }

        private void ProcessChanges()
        {
            if (this.Configuration != null)
            {
                SinkPostProcessStorage postProcess = new SinkPostProcessStorage(this.Provider.Sink, this.Configuration);

                if (!postProcess.ItemsToProcess)
                {
                    MessageBox.Show("There are no items to process at this time.", "Processing", MessageBoxButton.OK);
                }
                else
                {
                    StringBuilder message = new StringBuilder();
                    message.AppendFormat("This action will move images to new locations on your disk. Use the Process Changes button to view the actions that will be taken {0}{0}", Environment.NewLine);
                    message.AppendFormat("If all moves are succesfully completed, the history of your changes will be cleared. {0}{0}", Environment.NewLine);
                    message.AppendFormat("Would you like to continue with this processing step?");

                    if (MessageBox.Show(message.ToString(), "Process Changes", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        // Process the changes
                        if (postProcess.Process())
                        {
                            MessageBox.Show("Processing complete, provider will be reset", "Processing Complete", MessageBoxButton.OK);

                            // FOrce updates to clear sink and reset UI
                            //this.OnConfigurationSaved?.Invoke(this.Provider);
                            //this.OnSourceDataUpdated?.Invoke(this.Provider);
                        }
                        else
                        {
                            StringBuilder errorstatus = new StringBuilder();
                            foreach (String err in postProcess.Status)
                            {
                                errorstatus.AppendFormat(String.Format("{0}{1}", err, Environment.NewLine));
                            }
                            MessageBox.Show(errorstatus.ToString(), "Processing Errors Occured", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }

        }
        #endregion

        private void AcquireContent()
        {
            this.Collect();
            this.OnSourceDataUpdated?.Invoke(this.Provider);
        }

        private void Seed()
        {
            this.ConfigurationTextStorageAccount.Text = this.Configuration.StorageAccount;
            this.ConfigurationTextStorageAccountKey.Text = this.Configuration.StorageAccountKey;
            this.ConfigurationTextStorageContainer.Text = this.Configuration.StorageContainer;
            this.ConfigurationTextBlobPrefix.Text = this.Configuration.BlobPrefix;
            this.ConfigurationTextFileExtension.Text = this.Configuration.FileType;
            this.ConfigurationTextFileCount.Text = this.Configuration.FileCount.ToString();
            this.ConfigurationTextLocalDirectory.Text = this.Configuration.RecordLocation;

            this.MultiClassSelections[(this.Configuration.MultiClass ? 1 : 0)].IsChecked = true;
        }

        private void Collect()
        {
            if (MessageBox.Show("Saving configuration will delete the information saved by this source, do you want to continue?", "Save Configuration", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {

                this.Configuration.StorageAccount = this.ConfigurationTextStorageAccount.Text.Trim();
                this.Configuration.StorageAccountKey = this.ConfigurationTextStorageAccountKey.Text.Trim();
                this.Configuration.StorageContainer = this.ConfigurationTextStorageContainer.Text.Trim();
                this.Configuration.BlobPrefix = this.ConfigurationTextBlobPrefix.Text.Trim();
                this.Configuration.FileType = this.ConfigurationTextFileExtension.Text.Trim();
                this.Configuration.RecordLocation = this.ConfigurationTextLocalDirectory.Text.Trim();
                this.Configuration.MultiClass = !this.Negative.IsChecked.Value;

                try
                {
                    this.Configuration.FileCount = int.Parse(this.ConfigurationTextFileCount.Text.Trim());
                }
                catch
                {
                    // Just set a default and seed it
                    this.Configuration.FileCount = 1000;
                    this.Seed();
                }

                OnConfigurationSaved?.Invoke(this.Provider);

                this.ModifyChangeButtons();
            }
            else
            {
                this.Seed();
            }
        }

        private void ConfigurationSelectLogFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    this.ConfigurationTextLocalDirectory.Text = dialog.SelectedPath;
                }
            }
        }
    }
}
