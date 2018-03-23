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

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// The popup for a specific item from the MultiImageControl
    /// </summary>
    public partial class MultiInstanceDetailWindow : Window
    {
        private IMultiImageDataSource Source { get; set; }
        private CurrentItem Item { get; set; }
        private Image ItemImage { get; set; }
        private List<String> Classifications { get; set; }

        public MultiInstanceDetailWindow(
            IMultiImageDataSource source, 
            CurrentItem item, 
            Image itemImage,
            List<String> classifications)
        {
            InitializeComponent();

            this.Title = item.CurrentSource.Name;
            this.Source = source;
            this.Item = item;
            this.Classifications = classifications;

            // Create a copy of the image but you have to 
            // set width and height or it won't zoom
            this.ItemImage = new Image();
            this.ItemImage.Source = itemImage.Source;
            this.ItemImage.Width = itemImage.Width;
            this.ItemImage.Height = itemImage.Height;

            this.ScaleImage();
            this.ImagePanel.Children.Add(this.ItemImage);

            this.AddPanelItems();

            this.ButtonDownZoom.Click += (o, e) => ZoomOut();
            this.ButtonUpZoom.Click += (o, e) => ZoomIn();

            this.CloseWindow.Click += (o, e) => { this.Close(); };
        }

        private void ZoomOut()
        {
            if(this.ItemImage != null)
            {
                this.ItemImage.Height -= this.ItemImage.ActualHeight * .2;
                this.ItemImage.Width -= this.ItemImage.ActualWidth * .2;
            }
        }

        private void ZoomIn()
        {
            if (this.ItemImage != null)
            {
                this.ItemImage.Height += this.ItemImage.ActualHeight * .2;
                this.ItemImage.Width += this.ItemImage.ActualWidth * .2;
            }
        }

        private void AddPanelItems()
        {
            this.ClassificationPanel.Children.Clear();
            ClassificationCheckboxPanelHelper.PopulateSelectionPanel(
                this.Source,
                this.ClassificationPanel,
                this.Classifications,
                this.OnSelectionsChanged);

            ClassificationCheckboxPanelHelper.MakeSelection(
                this.ClassificationPanel,
                this.Item.CurrentSource);
        }

        private void OnSelectionsChanged()
        {
            this.Item.CurrentSource.Classifications = ClassificationCheckboxPanelHelper.CollectSelections(this.ClassificationPanel);

            if(this.Item.CurrentSource.Classifications.Count == 0)
            {
                // Back to the original
                this.Item.CurrentSource.Classifications.Add(this.Source.CurrentContainerAsClassification);
            }

            this.Source.UpdateSourceFile(this.Item.CurrentSource);
        }

        private void ScaleImage()
        {
            // We know the starting size of this screen but let's just do the math
            double scaleDownHeight = 0;
            double scaleDownWidth = 0;
            if (this.Item.OriginalSize.Height > this.Height)
            {
                scaleDownHeight = (this.Height - 100) / this.Item.OriginalSize.Height;
            }

            if (this.Item.OriginalSize.Width > this.Width)
            {
                scaleDownWidth = (this.Width - 200) / this.Item.OriginalSize.Width;
            }

            double scaleDown = (scaleDownHeight > scaleDownWidth) ? scaleDownWidth : scaleDownHeight;

            if (scaleDown > 0)
            {
                this.ItemImage.Height = this.Item.OriginalSize.Height * scaleDown;
                this.ItemImage.Width = this.Item.OriginalSize.Width * scaleDown;
            }

        }
    }
}
