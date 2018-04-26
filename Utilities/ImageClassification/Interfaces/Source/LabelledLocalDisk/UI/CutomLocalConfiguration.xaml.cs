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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.GlobalUtils.Processing;
using ImageClassifier.Interfaces.Source.LabelledLocalDisk.Configuration;

namespace ImageClassifier.Interfaces.Source.LabelledLocalDisk.UI
{
    /// <summary>
    /// Interaction logic for CutomLocalConfiguration.xaml
    /// </summary>
    public partial class CutomLocalConfiguration : UserControl, IConfigurationControlNotifications
    {
        #region Private Members
        private LocalSourceConfigurationUi ChildControl { get; set; }
        private LabelledLocalConfiguration Configuration { get; set; }
        #endregion

        #region IConfigurationControlNotifications
        public event OnConfigurationUpdatedHandler OnConfigurationSaved;
        public event OnUpdateSourceData OnSourceDataUpdated;
        #endregion

        #region Public Members
        /// <summary>
        /// The IDataSource behind this configuraiton
        /// </summary>
        public IDataSource Provider { get; private set; }
        #endregion

        public CutomLocalConfiguration(IDataSource source, LabelledLocalConfiguration configuration)
        {
            InitializeComponent();

            this.Provider = source;
            this.Configuration = configuration;

            this.ChildControl = new LocalSourceConfigurationUi(source, this.Configuration.LocalConfiguration);
            this.ChildControl.OnConfigurationSaved += ChildConfigurationSaved;
            this.ChildControl.OnSourceDataUpdated += ChildSourceUpdated;

            this.LocalSourceUiPanel.Children.Clear();
            this.LocalSourceUiPanel.Children.Add(this.ChildControl);

            int itemidx = 0;
            foreach (object entry in this.BatchSize.Items)
            {
                if (entry is ComboBoxItem)
                {
                    if (String.Compare((entry as ComboBoxItem).Content.ToString(), this.Configuration.BatchSize.ToString()) == 0)
                    {
                        this.BatchSize.SelectedIndex = itemidx;
                        break;
                    }
                }
                itemidx++;
            }

            // Hook buttons
            this.PreviewChangesButton.Click += (o, e) => PreviewChanges();
            this.ProcessChangesButton.Click += (o, e) => ProcessChanges();
            this.ModifyChangeButtons();
        }

        #region Preview Helpers
        private void ModifyChangeButtons()
        {
            bool enableChanges = !this.Configuration.LocalConfiguration.MultiClass;
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
                        this.ChildConfigurationSaved(this.Provider);
                        this.ChildSourceUpdated(this.Provider);
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

        #region Private Helpers - child control hooks
        /// <summary>
        /// Event hooked from childe AzureStoreageConfigurationUi so that it can be broadcast out
        /// to listeners.
        /// </summary>
        private void ChildSourceUpdated(IDataSource source)
        {
            this.OnSourceDataUpdated?.Invoke(source);
        }

        /// <summary>
        /// Event hooked from childe AzureStoreageConfigurationUi so that it can collect additional 
        /// information before being broadcast outto listeners.
        /// </summary>
        private void ChildConfigurationSaved(IDataSource source)
        {
            this.ModifyChangeButtons();
            this.Configuration.BatchSize = int.Parse((this.BatchSize.SelectedItem as ComboBoxItem).Content.ToString());
            this.OnConfigurationSaved?.Invoke(source);
        }
        #endregion

    }
}
