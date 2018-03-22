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

namespace ImageClassifier.Interfaces.Source.BlobSource.UI
{
    /// <summary>
    /// Interaction logic for AzureStorageConfigurationUi.xaml
    /// </summary>
    public partial class AzureStorageConfigurationUi : UserControl
    {
        public event OnConfigurationUpdatedHandler OnConfigurationSaved;
        public event OnUpdateSourceData OnSourceDataUpdated;

        public IDataSource Provider { get; private set; }

        public AzureBlobStorageConfiguration Configuration { get; private set; }

        public AzureStorageConfigurationUi(IDataSource source, AzureBlobStorageConfiguration config)
        {
            InitializeComponent();
            this.Provider = source;
            this.Configuration = config;
            this.ConfigurationButtonSave.Click += (o, e) => this.Collect();
            this.ConfigurationButtonAquireContent.Click += (o, e) => this.AcquireContent();
            this.ConfigurationButtonDirectory.Click += (o, e) => ConfigurationSelectLogFolder();
            this.Seed();
        }

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
        }

        private void Collect()
        {
            this.Configuration.StorageAccount = this.ConfigurationTextStorageAccount.Text;
            this.Configuration.StorageAccountKey = this.ConfigurationTextStorageAccountKey.Text;
            this.Configuration.StorageContainer = this.ConfigurationTextStorageContainer.Text;
            this.Configuration.BlobPrefix = this.ConfigurationTextBlobPrefix.Text;
            this.Configuration.FileType = this.ConfigurationTextFileExtension.Text;
            this.Configuration.RecordLocation = this.ConfigurationTextLocalDirectory.Text;
            try
            {
                this.Configuration.FileCount = int.Parse(this.ConfigurationTextFileCount.Text);
            }
            catch
            {
                // Just set a default and seed it
                this.Configuration.FileCount = 1000;
                this.Seed();
            }

            OnConfigurationSaved?.Invoke(this.Provider);
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
