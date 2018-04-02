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
using System.Windows;
using System.Windows.Controls;
using ImageClassifier.Interfaces.GlobalUtils.Configuration;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// Interaction logic for LocalSourceConfigurationUi.xaml
    /// </summary>
    #pragma warning disable CS0067
    public partial class LocalSourceConfigurationUi : UserControl
    {
        public event OnConfigurationUpdatedHandler OnConfigurationSaved;
        public event OnUpdateSourceData OnSourceDataUpdated;

        public IDataSource Provider { get; private set; }
        public LocalDiskSourceConfiguration Configuration { get; private set; }

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

            this.Seed();
        }

        private void Seed()
        {
            this.ConfigurationTextFileExtension.Text = String.Join(",", this.Configuration.FileTypes);
            this.ConfigurationTextLocalDirectory.Text = this.Configuration.RecordLocation;
        }

        private void Collect()
        {
            if (MessageBox.Show("Saving configuration will delete the information saved by this source, do you want to continue?", "Save Configuration", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                this.Configuration.FileTypes = new System.Collections.Generic.List<string>(this.ConfigurationTextFileExtension.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                this.Configuration.RecordLocation = this.ConfigurationTextLocalDirectory.Text;

                this.OnConfigurationSaved?.Invoke(this.Provider);
                this.OnSourceDataUpdated?.Invoke(this.Provider);
            }
            else
            {
                this.Seed();
            }
        }

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

    }
}
