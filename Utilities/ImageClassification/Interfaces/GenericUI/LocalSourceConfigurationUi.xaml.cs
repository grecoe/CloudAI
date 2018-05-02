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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using ImageClassifier.Interfaces.GlobalUtils.Processing;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// Interaction logic for LocalSourceConfigurationUi.xaml
    /// </summary>
    #pragma warning disable CS0067
    public partial class LocalSourceConfigurationUi : UserControl, IConfigurationControlNotifications
    {
        #region IConfigurationControlNotifications
        public event OnConfigurationUpdatedHandler OnConfigurationSaved;
        public event OnUpdateSourceData OnSourceDataUpdated;
        #endregion 

        /// <summary>
        /// Default file extensions to use if not specified
        /// </summary>
        private const string DefaultExtensions = ".jpg,.tif,.tiff,.png,.gif";

        /// <summary>
        /// Toggle buttons for the multi/single class selections
        /// </summary>
        private List<ToggleButton> MultiClassSelections { get; set; }

        /// <summary>
        /// Data source provider being used
        /// </summary>
        public IDataSource Provider { get; private set; }

        /// <summary>
        /// Configuration object for locak disk source settings
        /// </summary>
        public LocalDiskSourceConfiguration Configuration { get; private set; }

        /// <summary>
        /// Additinal constructor with the purpose of being able to use the XAML editor, not to be used
        /// during execution.
        /// </summary>
        public LocalSourceConfigurationUi()
        {
            InitializeComponent();
        }

        public LocalSourceConfigurationUi(IDataSource source, LocalDiskSourceConfiguration config)
        {
            InitializeComponent();
            this.Provider = source;
            this.Configuration = config;

            this.ConfigurationButtonSave.Click += (o, e) => this.Collect();
            this.ConfigurationButtonDirectory.Click += (o, e) => ConfigurationSelectSourceFolder();

            this.MultiClassSelections = new List<ToggleButton>() { this.Negative, this.Affirmitive };

            this.Seed();

            // Hook buttons
            this.PreviewChangesButton.Click += (o, e) => PreviewChanges();
            this.ProcessChangesButton.Click += (o, e) => ProcessChanges();
            this.ModifyChangeButtons();
        }

        #region Preview Helpers
        private void ModifyChangeButtons()
        {
            bool enableChanges = !this.Configuration.MultiClass;
            this.PreviewChangesButton.IsEnabled = enableChanges;
            this.ProcessChangesButton.IsEnabled = enableChanges;
        }

        private void PreviewChanges()
        {
            SinkPostProcess postProcess = new SinkPostProcess(this.Provider.Sink);
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

        private void ProcessChanges()
        {
            SinkPostProcess postProcess = new SinkPostProcess(this.Provider.Sink);

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
                        this.OnConfigurationSaved?.Invoke(this.Provider);
                        this.OnSourceDataUpdated?.Invoke(this.Provider);
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
        #endregion

        #region Setup and save
        /// <summary>
        /// Seed the UI with any data
        /// </summary>
        private void Seed()
        {
            this.ConfigurationTabTextAnnotationTags.Text = string.Join(",", this.Configuration.Classifications);
            this.ConfigurationTextFileExtension.Text = String.Join(",", this.Configuration.FileTypes);
            if(String.IsNullOrEmpty(this.ConfigurationTextFileExtension.Text))
            {
                this.ConfigurationTextFileExtension.Text = DefaultExtensions;
            }
            this.ConfigurationTextLocalDirectory.Text = this.Configuration.RecordLocation;
            this.MultiClassSelections[(this.Configuration.MultiClass ? 1 : 0)].IsChecked = true;
        }

        /// <summary>
        /// Obtain changes and invoke delegates.
        /// </summary>
        private void Collect()
        {
            if(String.IsNullOrEmpty(this.ConfigurationTextLocalDirectory.Text) || !System.IO.Directory.Exists(this.ConfigurationTextLocalDirectory.Text))
            {
                MessageBox.Show("Invalid directory supplied for image sources. Check the setting and try again.", "Invalid Directory", MessageBoxButton.OK);
            }
            else if (String.IsNullOrEmpty(this.ConfigurationTextFileExtension.Text))
            {
                MessageBox.Show("Supply at least one file extension. Many entries are separated with a comma.", "Invalid File Extension", MessageBoxButton.OK);
            }
            else if (MessageBox.Show("Saving configuration will delete the information saved by this source, do you want to continue?", "Save Configuration", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                this.Configuration.Classifications =
                    new List<string>(this.ConfigurationTabTextAnnotationTags.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                this.Configuration.FileTypes = new System.Collections.Generic.List<string>(this.ConfigurationTextFileExtension.Text.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                this.Configuration.RecordLocation = this.ConfigurationTextLocalDirectory.Text.Trim();
                this.Configuration.MultiClass = !this.Negative.IsChecked.Value;

                if (!System.IO.Directory.Exists(this.Configuration.RecordLocation))
                {
                    String message = String.Format("{0}{1}{2}",
                        "The supplied directory does not exist:",
                        Environment.NewLine,
                        this.Configuration.RecordLocation);
                    MessageBox.Show("Supplied", "Directory Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    this.OnConfigurationSaved?.Invoke(this.Provider);
                    this.OnSourceDataUpdated?.Invoke(this.Provider);
                }

                this.ModifyChangeButtons();
            }
            else
            {
                this.Seed();
            }
        }

        /// <summary>
        /// Directory selector for where the data is coming from.
        /// </summary>
        private void ConfigurationSelectSourceFolder()
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
        #endregion

    }
}
