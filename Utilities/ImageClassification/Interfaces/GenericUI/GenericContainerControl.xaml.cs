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
using ImageClassifier.Interfaces.GenericUI.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// Interaction logic for GenericContainerControl.xaml
    /// </summary>
    public partial class GenericContainerControl : UserControl, IContainerControl
    {
        #region IContainerControl
        public string CurrentContainer { get { return (this.DataSource != null ? this.DataSource.CurrentContainer : String.Empty); } }

        public UIElement Control { get { return this; } }

        public IDataSource DataSource { get; private set; }

        public event OnContainerChangedHandler OnContainerChanged;

        public void Refresh()
        {
           this.Dispatcher.Invoke(() =>
           {
               this.Initialize();
           });
        }

        #endregion

        public GenericContainerControl(IDataSource source)
        {
            InitializeComponent();
            this.DataSource = source;
            this.SourceContainerCombo.SelectionChanged += (o, e) => ContainerSelectionChanged();

            this.Initialize();
        }

        /// <summary>
        /// Called when the current selection changes in the container combo box
        /// </summary>
        private void ContainerSelectionChanged()
        {
            ContainerComboItem item = null;
            if ((item = this.SourceContainerCombo.SelectedItem as ContainerComboItem) != null &&
                this.DataSource != null)
            {
                this.DataSource.SetContainer(item.SourceContainer);

                this.OnContainerChanged?.Invoke(this, item);
            }
        }

        /// <summary>
        /// Initialzes the control by updating the list of containers and firing off the selection changed event.
        /// </summary>
        private void Initialize()
        {
            this.SourceContainerCombo.Items.Clear();

            if (this.DataSource != null && this.DataSource.Containers != null)
            {
                foreach (String file in this.DataSource.Containers)
                {
                    this.SourceContainerCombo.Items.Add(new ContainerComboItem(this.DataSource.SourceType, file));
                }

                this.SourceContainerCombo.SelectedIndex = 0;
            }
        }

    }
}
