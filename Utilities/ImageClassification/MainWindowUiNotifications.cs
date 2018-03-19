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

using ImageClassifier.UIUtils;
using System;
using System.Collections.Generic;
using System.Windows;
using ImageClassifier.Interfaces;

namespace ImageClassifier
{
    public partial class MainWindow
    {
        #region Status Bar Events
        private void StatusBarClearStatus()
        {
            this.StatusBarCollection.Text = String.Empty;
            this.StatusBarLocationStatus.Text = String.Empty;
        }

        private void StatusBarJumpToImage()
        {
            String error = String.Empty;
            if (this.SelectedDataSource != null)
            {
                try
                {
                    int index = int.Parse(this.StatusTextJumpTo.Text);
                    this.SelectedDataSource.JumpToSourceFile(index);
                    //TODO:DELETE how to hook this this.ClassificationTabNextImage();
                    this.SelectedDataSource.ImageControl.ShowNext();
                }
                catch (Exception)
                {
                    error = "Jump to index must be a number";
                }
            }
            else
            {
                error = "A source provider must be present";
            }

            if (!String.IsNullOrEmpty(error))
            {
                System.Windows.MessageBox.Show(error, "Jump To Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Configuration UI Events
        private void SourceProviderChanged()
        {
            if (this.ConfigurationTabSourceProviderCombo.SelectedItem is DataSourceComboItem)
            {
                this.StatusBarClearStatus();

                // Unhook control callbacks......
                if (this.SelectedDataSource != null)
                {
                    this.SelectedDataSource.ContainerControl.OnContainerChanged -= this.IContainerControlContainerChanged;
                    if (this.SelectedDataSource.ImageControl is ISingleImageControl)
                    {
                        ((ISingleImageControl)this.SelectedDataSource.ImageControl).ImageChanged -= this.ISingleImageControlFileChanged;
                    }
                }

                this.SelectedDataSource = (this.ConfigurationTabSourceProviderCombo.SelectedItem as DataSourceComboItem).Source;

                this.ConfigurationContext.DefaultProvider = this.SelectedDataSource.Name;
                this.ConfigurationContext.Save();

                this.InitializeUi(true);

                // Hook control callbacks......
                if (this.SelectedDataSource.ImageControl is ISingleImageControl)
                {
                    ((ISingleImageControl)this.SelectedDataSource.ImageControl).ImageChanged += this.ISingleImageControlFileChanged;
                }

                this.SelectedDataSource.ContainerControl.OnContainerChanged += this.IContainerControlContainerChanged;
                this.SelectedDataSource.ContainerControl.Refresh();
            }
        }

        #endregion

        #region Classification Tab
        private List<String> ClassificationPanelCollectSelections()
        {
            List<String> annotations = new List<string>();

            foreach (UIElement ui in this.ClassificationTabSelectionPanel.Children)
            {
                System.Windows.Controls.Primitives.ToggleButton childBox = null;
                if ((childBox = ui as System.Windows.Controls.Primitives.ToggleButton) != null)
                {
                    if (childBox.IsChecked.HasValue && childBox.IsChecked.Value)
                    {
                        annotations.Add(childBox.Content.ToString());
                    }
                }
            }

            return annotations;
        }

        private void ClassificationPanelMakeSelections(SourceFile image)
        {
            foreach (UIElement ui in this.ClassificationTabSelectionPanel.Children)
            {
                System.Windows.Controls.Primitives.ToggleButton childBox = null;
                if ((childBox = ui as System.Windows.Controls.Primitives.ToggleButton) != null)
                {
                    childBox.IsChecked = image.Classifications.Contains(childBox.Content.ToString());
                }
            }
        }
        #endregion Annotation Tab Events
    }
}
