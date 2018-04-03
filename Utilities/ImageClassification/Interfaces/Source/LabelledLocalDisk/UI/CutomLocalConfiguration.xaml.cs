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
using ImageClassifier.Interfaces.Source.LabelledLocalDisk.Configuration;
using System;
using System.Windows.Controls;

namespace ImageClassifier.Interfaces.Source.LabelledLocalDisk.UI
{
    /// <summary>
    /// Interaction logic for CutomLocalConfiguration.xaml
    /// </summary>
    public partial class CutomLocalConfiguration : UserControl
    {
        #region Private Members
        private LocalSourceConfigurationUi ChildControl { get; set; }
        private LabelledLocalConfiguration Configuration { get; set; }
        #endregion

        public event OnConfigurationUpdatedHandler OnConfigurationSaved;
        public event OnUpdateSourceData OnSourceDataUpdated;

        public IDataSource Provider { get; private set; }

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
        }

        private void ChildSourceUpdated(IDataSource source)
        {
            this.OnSourceDataUpdated?.Invoke(source);
        }

        private void ChildConfigurationSaved(IDataSource source)
        {
            this.Configuration.BatchSize = int.Parse((this.BatchSize.SelectedItem as ComboBoxItem).Content.ToString());
            this.OnConfigurationSaved?.Invoke(source);
        }

    }
}
