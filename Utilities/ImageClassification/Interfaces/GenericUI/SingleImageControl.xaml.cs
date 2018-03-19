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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImageClassifier.UIUtils;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// Interaction logic for SingleImageControl.xaml
    /// </summary>
    public partial class SingleImageControl : UserControl, ISingleImageControl
    {
        public SingleImageControl(IDataSource source)
        {
            InitializeComponent();

            this.DataSource = source;
            this.CurrentSourceFile = new CurrentItem();

            this.ButtonNext.Click += (o, e) => NextImage();
            this.ButtonPrevious.Click += (o, e) => PreviousImage();
            this.ButtonUpZoom.Click += (o, e) => ZoomImage();
            this.ButtonDownZoom.Click += (o, e) => ZoomOutImage();

        }

        #region ISingleImageControl
        public event OnImageChanged ImageChanged;

        public IEnumerable<KeyBinding> Bindings
        {
            get
            {
                List<KeyBinding> bindings = new List<KeyBinding>();
                bindings.Add(new KeyBinding(
                    new ImageChangeCommand(this.ButtonNext, this.NextImage),
                    Key.N,
                    ModifierKeys.Control));

                bindings.Add(new KeyBinding(
                    new ImageChangeCommand(this.ButtonPrevious, this.PreviousImage),
                    Key.P,
                    ModifierKeys.Control));

                return bindings;
            }
        }

        public UIElement ParentControl { get; set; }

        public CurrentItem CurrentSourceFile { get; private set; }

        public IDataSource DataSource { get; private set; }

        public UIElement Control { get { return this; } }

        public void UpdateClassifications(List<string> classifications)
        {
            if (this.CurrentSourceFile != null && this.CurrentSourceFile.CurrentSource != null)
            {
                this.CurrentSourceFile.CurrentSource.Classifications = classifications;
                this.DataSource.UpdateSourceFile(this.CurrentSourceFile.CurrentSource);
            }
        }

        public void Clear()
        {
            this.ImageLabel.Text = String.Empty;
            this.ImageSizeData.Text = String.Empty;
            this.ImagePanel.Children.Clear();
            this.ButtonNext.IsEnabled = this.DataSource.CanMoveNext;
            this.ButtonPrevious.IsEnabled = this.DataSource.CanMovePrevious;
        }

        /// <summary>
        /// Fast forward through the collection to the first un-tagged item
        /// </summary>
        public void FastForward()
        {
            if (this.DataSource.Sink != null &&
                this.DataSource.CurrentContainerCollectionCount > 0)
            {
                int imageIdx = 1;
                this.DataSource.JumpToSourceFile(imageIdx);
                foreach (String itemName in this.DataSource.CurrentContainerCollectionNames)
                {
                    if (!this.DataSource.Sink.ItemHasBeenScored(this.DataSource.CurrentContainer, itemName))
                    {
                        break;
                    }
                    imageIdx++;
                }
                this.DataSource.JumpToSourceFile(imageIdx);
            }

            this.ShowNext();

        }

        public void ShowNext()
        {
            this.NextImage();
        }

        #endregion

        #region Navigation
        private void ZoomImage()
        {
            foreach (UIElement item in this.ImagePanel.Children)
            {
                Image child = null;
                if ((child = item as Image) != null)
                {
                    child.Height = child.Height * 1.2;
                    child.Width = child.Width * 1.2;

                    this.CurrentSourceFile.CurrentSize = new System.Drawing.Size((int)child.Width, (int)child.Height);
                    this.UpdateSizeInformation();
                }
            }
        }

        private void ZoomOutImage()
        {
            foreach (UIElement item in this.ImagePanel.Children)
            {
                Image child = null;
                if ((child = item as Image) != null)
                {
                    child.Height = child.Height * .8;
                    child.Width = child.Width * .8;

                    this.CurrentSourceFile.CurrentSize = new System.Drawing.Size((int)child.Width, (int)child.Height);
                    this.UpdateSizeInformation();
                }
            }
        }

        private void UpdateSizeInformation()
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
            this.ImageSizeData.Text = imageSizeInfo;
        }

        private void MoveImage(SourceFile file)
        {
            if (this.CurrentSourceFile != null && this.CurrentSourceFile.CurrentSource != null)
            {
                if (this.DataSource.DeleteSourceFilesWhenComplete &&
                    System.IO.File.Exists(this.CurrentSourceFile.CurrentSource.DiskLocation))
                {
                    System.IO.File.Delete(this.CurrentSourceFile.CurrentSource.DiskLocation);
                }
            }
            this.CurrentSourceFile.CurrentSource = file;

            // Get the size of the parent
            double height = 100;
            double width = 100;

            FrameworkElement fe = this.Parent as FrameworkElement;
            if(fe != null)
            {
                height = fe.ActualHeight;
                width = fe.ActualWidth;

                // Have to account for height of other controls here
                height -= ((this.ImageLabel.ActualHeight + this.ImageSizeData.ActualHeight + this.NavigationPanel.ActualHeight));
            }

            // Clear the panel 
            this.ImageLabel.Text = String.Empty;
            this.ImagePanel.Children.Clear();

            System.IO.MemoryStream downloadFile = this.GetFileStream(file.DiskLocation);

            // Call the image changed so that the base can update what it needs.
            this.ImageChanged?.Invoke(file);

            // Add the image to the stack panel for viewing and the image name
            this.ImageLabel.Text = String.Format("Current Image: {0}", file.Name);
            this.ImagePanel.Children.Add(this.CreateUiImage(width, height, downloadFile));

            // UPdate the image information 
            this.UpdateSizeInformation();

            // Update the navigation buttons
            this.ButtonNext.IsEnabled = this.DataSource.CanMoveNext;
            this.ButtonPrevious.IsEnabled = this.DataSource.CanMovePrevious;
        }

        private void PreviousImage()
        {
            if (this.DataSource != null && this.DataSource.CanMovePrevious)
            {
                this.MoveImage(this.DataSource.PreviousSourceFile());
            }
        }

        private void NextImage()
        {
            if (this.DataSource != null && this.DataSource.CanMoveNext)
            {
                this.MoveImage(this.DataSource.NextSourceFile());
            }
        }
        #endregion

        #region Image Helpers
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

                    double use = (widthChange > heightChange) ? heightChange : widthChange;
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
        #endregion  Image Helpers

    }
}
