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
using ImageClassifier.Interfaces.GlobalUtils.Persistence;
using ImageClassifier.MainWindowUtilities;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ImageClassifier.Interfaces.GenericUI
{
    public delegate void OnImageGroupSelectedHandler(CurrentItem item);

    /// <summary>
    /// The grid element in the multiimagecontrol
    /// </summary>
    public partial class MultiImageInstance : UserControl
    {
        private IMultiImageDataSource Source { get; set; }
        public CurrentItem Item { get; set; }
        private Image ItemImage { get; set; }
        private double ParentHeight { get; set; }
        private double ParentWidth { get; set; }
        private List<String> Classifications { get; set; }

        public event OnImageGroupSelectedHandler OnImageGroupSelected;

        public MultiImageInstance(
            IMultiImageDataSource source, 
            CurrentItem item, 
            double parentHeight, 
            double parentWidth,
            List<String> classifications)
        {
            InitializeComponent();

            this.Source = source;
            this.Item = item;
            this.ParentHeight = ( parentHeight > 0) ? parentHeight : 300;
            this.ParentWidth = (parentWidth > 0) ? parentWidth : 300; ;

            this.Classifications = classifications;

           // this.ImageName.Text = this.Item.CurrentSource.Name;
            this.UpdateLabels();

            this.ImageGrid.MouseDown += ImageSelected;
            //this.ImagePanel.MouseDown += ImageSelected;

            this.ThreadCollectImage(null);
        }

        /// <summary>
        /// Update the list of labels under the image
        /// </summary>
        public void UpdateLabels()
        {
            this.ImageLabels.Text = String.Join(Environment.NewLine, this.Item.CurrentSource.Classifications);
        }

        public void ResetBackground()
        {
            this.ImageGrid.Background = new SolidColorBrush(Colors.LightBlue);
        }

        /// <summary>
        /// Capture the mouse down on the Image to show the MultiImageDetailWindow so that the user
        /// can modify the classifications.
        /// </summary>
        private void ImageSelected(object sender, MouseButtonEventArgs e)
        {
            if(e.ButtonState == MouseButtonState.Pressed)
            {
                // If control is selected, notify parent, otherwise show image
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    OnImageGroupSelected?.Invoke(this.Item);
                    this.ImageGrid.Background = new SolidColorBrush(Colors.CornflowerBlue);
                }
                else
                {
                    OnImageGroupSelected?.Invoke(null);

                    MultiInstanceDetailWindow window =
                        new MultiInstanceDetailWindow(this.Source, this.Item, this.ItemImage, this.Classifications);
                    window.ShowDialog();

                    this.UpdateLabels();
                }
            }
        }

        /// <summary>
        /// Intended for a thread to collect the image and display it on the screen.
        /// 
        /// Using as a thread routine is causing some issues, but there was no need to thread in the 
        /// first place since the image has already been downloaded.
        /// </summary>
        /// <param name="unused"></param>
        private void ThreadCollectImage(object unused)
        {
            // Give up 30% of the height to the text boxes
            double height = this.ParentHeight * .75;
            double width = this.ParentWidth * .75;

            double usable = Math.Min(height, width);

            try
            {
                this.ImagePanel.Dispatcher.Invoke(() =>
                {
                    System.IO.MemoryStream downloadFile = FileUtils.GetFileStream(this.Item.CurrentSource.DiskLocation);
                    this.ItemImage = ElementCreation.CreateUiImage(this.Item, usable, usable, downloadFile);
                    this.ImagePanel.Children.Add(this.ItemImage);
                });

                this.ImageSize.Dispatcher.Invoke(() =>
                {
                    this.ImageSize.Text = String.Format("{0}w x {1}h", this.Item.OriginalSize.Width, this.Item.OriginalSize.Height);
                });
            }
            catch(Exception ex)
            {
                // Eat the error as it's likely the file was removed from storage, but interestingly, only got this when I was 
                // trying to thread this load.
            }
        }
    }
}
