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
using System.Windows.Controls;
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
                    this.ClassificationTabNextImage();
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
                this.SelectedDataSource = (this.ConfigurationTabSourceProviderCombo.SelectedItem as DataSourceComboItem).Source;
                this.ConfigurationContext.DefaultProvider = this.SelectedDataSource.Name;
                this.ConfigurationContext.Save();
                this.InitializeUi(true);
            }
        }
        #endregion

        #region Annotation Tab Events
        private void ClassificationTabContainerChanged(object sender, SelectionChangedEventArgs args)
        {
            this.StatusBarClearStatus();
            this.ClassificationTabImageLabel.Text = String.Empty;
            this.ClassificationTabImageSizeData.Text = String.Empty;
            this.ClassificationTabImagePanel.Children.Clear();
            this.ClassificationTabNavigationButtonNext.IsEnabled = false;
            this.ClassificationTabNavigationButtonPrevious.IsEnabled = false;

            if (this.SelectedDataSource != null &&
                this.CurrentSourceFile != null &&
                this.SelectedDataSource.DeleteSourceFilesWhenComplete &&
                System.IO.File.Exists(this.CurrentSourceFile.CurrentSource.DiskLocation))
            {
                System.IO.File.Delete(this.CurrentSourceFile.CurrentSource.DiskLocation);
            }

            ContainerComboItem item = null;
            if ((item = this.ClassificationTabContainerCombo.SelectedItem as ContainerComboItem) != null)
            {
                this.StatusBarCollection.Text = item.ToString();

                if (this.SelectedDataSource != null)
                {
                    this.SelectedDataSource.SetContainer(item.SourceContainer);

                    this.ClassificationTabNavigationButtonNext.IsEnabled = this.SelectedDataSource.CanMoveNext;
                    this.ClassificationTabNavigationButtonPrevious.IsEnabled = this.SelectedDataSource.CanMovePrevious;
                }

                // Now fast forward through what we have.
                if(this.SelectedDataSource.Sink != null &&
                    this.SelectedDataSource.CurrentContainerCollectionCount > 0 )
                {
                    int imageIdx = 1;
                    this.SelectedDataSource.JumpToSourceFile(imageIdx);
                    foreach(String itemName in this.SelectedDataSource.CurrentContainerCollectionNames)
                    {
                        if(!this.SelectedDataSource.Sink.ItemHasBeenScored(this.SelectedDataSource.CurrentContainer, itemName))
                        {
                            break;
                        }
                        imageIdx++;
                    }
                    this.SelectedDataSource.JumpToSourceFile(imageIdx);
                }

                this.ClassificationTabNextImage();
            }
        }

        private void ClassificationTabZoomImage()
        {
            foreach (UIElement item in this.ClassificationTabImagePanel.Children)
            {
                Image child = null;
                if ((child = item as Image) != null)
                {
                    child.Height = child.Height * 1.2;
                    child.Width = child.Width * 1.2;

                    this.CurrentSourceFile.CurrentSize = new System.Drawing.Size((int)child.Width, (int)child.Height);
                    this.ClassificationTabUpdateSizeInformation();
                }
            }
        }

        private void ClassificationTabZoomOutImage()
        {
            foreach (UIElement item in this.ClassificationTabImagePanel.Children)
            {
                Image child = null;
                if ((child = item as Image) != null)
                {
                    child.Height = child.Height * .8;
                    child.Width = child.Width * .8;

                    this.CurrentSourceFile.CurrentSize = new System.Drawing.Size((int)child.Width, (int)child.Height);
                    this.ClassificationTabUpdateSizeInformation();
                }
            }
        }

        private void ClassificationTabUpdateSizeInformation()
        {
            String imageSizeInfo = String.Empty;
            if (this.CurrentSourceFile != null)
            {
                imageSizeInfo = String.Format("Original Size: {0}{3}Current Size:{1}{3}Zoom Level: {2}%",
                    this.CurrentSourceFile.OriginalSize.ToString(),
                    this.CurrentSourceFile.CurrentSize.ToString(),
                    this.CurrentSourceFile.Zoom.ToString(),
                    Environment.NewLine);
            }
            this.ClassificationTabImageSizeData.Text = imageSizeInfo;
        }
        private void ClassificationTabMoveImage(SourceFile file)
        {
            if(this.CurrentSourceFile != null && this.CurrentSourceFile.CurrentSource != null)
            {
                this.CurrentSourceFile.CurrentSource.Classifications = this.AnnotationsPanelCollectSelections();
                this.SelectedDataSource.UpdateSourceFile(this.CurrentSourceFile.CurrentSource);

                if (this.SelectedDataSource.DeleteSourceFilesWhenComplete &&
                    System.IO.File.Exists(this.CurrentSourceFile.CurrentSource.DiskLocation))
                {
                        System.IO.File.Delete(this.CurrentSourceFile.CurrentSource.DiskLocation);
                }
            }
            this.CurrentSourceFile.CurrentSource = file;

            // Update the status bar
            this.StatusBarLocationStatus.Text =
                String.Format("Viewing {0} of {1} ",
                this.SelectedDataSource.CurrentContainerIndex + 1,
                this.SelectedDataSource.CurrentContainerCollectionCount);


            // Clear the stack panel
            double height = this.ClassificationTabSelectionPanel.ActualHeight;
            double width = this.ClassificationTabSelectionPanel.ActualWidth;
            this.ClassificationTabImageLabel.Text = String.Empty;
            this.ClassificationTabImagePanel.Children.Clear();

            System.IO.MemoryStream downloadFile = this.GetFileStream(file.DiskLocation);

            // Set up the annotations from the selected image
            this.AnnotationPanelSelectFromImage(file);

            // Add the image to the stack panel for viewing and the image name
            this.ClassificationTabImageLabel.Text = String.Format("Current Image: {0}", file.Name);
            this.ClassificationTabImagePanel.Children.Add(this.CreateUiImage(width, height, downloadFile));

            // UPdate the image information 
            this.ClassificationTabUpdateSizeInformation();

            // Update the navigation buttons
            this.ClassificationTabNavigationButtonNext.IsEnabled = this.SelectedDataSource.CanMoveNext;
            this.ClassificationTabNavigationButtonPrevious.IsEnabled = this.SelectedDataSource.CanMovePrevious;
        }

        private void ClassificationTabPreviousImage()
        {
            if (this.SelectedDataSource != null && this.SelectedDataSource.CanMovePrevious)
            {
                this.ClassificationTabMoveImage(this.SelectedDataSource.PreviousSourceFile());
            }
        }

        private void ClassificationTabNextImage()
        {
            if(this.SelectedDataSource != null && this.SelectedDataSource.CanMoveNext)
            {
                this.ClassificationTabMoveImage(this.SelectedDataSource.NextSourceFile());
            }
        }

        #region Annotation Tab Image Helpers
        private Image CreateUiImage(double parentWidth, double parentHeight, System.IO.MemoryStream stream)
        {
            System.Windows.Media.Imaging.BitmapImage bi = new System.Windows.Media.Imaging.BitmapImage();
            bi.BeginInit();
            bi.StreamSource = stream;
            bi.EndInit();

            Image newImage = new Image();
            newImage.Source = bi;
            newImage.Width = bi.Width;
            newImage.Height = bi.Height;
            newImage.Stretch = System.Windows.Media.Stretch.Fill;
            newImage.StretchDirection = StretchDirection.Both;


            this.CurrentSourceFile.OriginalSize = new System.Drawing.Size((int)bi.Width, (int)bi.Height);
            this.CurrentSourceFile.CurrentSize = new System.Drawing.Size((int)bi.Width, (int)bi.Height);

            if (parentWidth > 0 && parentHeight > 0)
            {
                if (
                    ((parentWidth < newImage.Width) && (parentHeight < newImage.Height)) ||
                    ((parentWidth > newImage.Width) && (parentHeight > newImage.Height))
                    )
                {
                    double widthChange = parentWidth / newImage.Width;
                    double heightChange = parentHeight / newImage.Height;

                    double use = (widthChange > heightChange) ? widthChange : heightChange ;
                    use = use * .8;

                    newImage.Width = newImage.Width * use;
                    newImage.Height = newImage.Height * use;
                    this.CurrentSourceFile.CurrentSize = new System.Drawing.Size((int)newImage.Width, (int)newImage.Height);
                }
            }
            return newImage;
        }

        private System.IO.MemoryStream GetFileStream(string fileLocation)
        {
            System.IO.MemoryStream returnStream = null;
            using (System.IO.FileStream stream = new System.IO.FileStream(fileLocation, System.IO.FileMode.Open))
            {
                returnStream = new System.IO.MemoryStream();
                stream.CopyTo(returnStream);
            }

            returnStream.Position = 0;
            return returnStream;

        }
        #endregion  Annotation Tab Image Helpers

        #region Annotation Tab Data Collection
        private List<String> AnnotationsPanelCollectSelections()
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

        private void AnnotationPanelSelectFromImage(SourceFile image)
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
        #endregion Annotation Tab Data Collection

        #endregion Annotation Tab Events
    }
}
