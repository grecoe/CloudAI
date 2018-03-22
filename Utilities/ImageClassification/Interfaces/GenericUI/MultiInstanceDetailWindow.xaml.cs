using ImageClassifier.UIUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// The popup for a specific item from the MultiImageControl
    /// </summary>
    public partial class MultiInstanceDetailWindow : Window
    {
        private CurrentItem Item { get; set; }
        private Image ItemImage { get; set; }

        public MultiInstanceDetailWindow(CurrentItem item, Image itemImage)
        {
            InitializeComponent();

            this.Title = item.CurrentSource.Name;

            this.Item = item;

            this.ItemImage = new Image();
            this.ItemImage.Source = itemImage.Source;

            this.ScaleImage();
            this.ImagePanel.Children.Add(this.ItemImage);

            this.AddPanelItems();

            this.ButtonDownZoom.Click += (o, e) => ZoomOut();
            this.ButtonUpZoom.Click += (o, e) => ZoomIn();

        }

        private void ZoomOut()
        {
            if(this.ItemImage != null)
            {
                this.ItemImage.Height -= this.ItemImage.Height * .2;
                this.ItemImage.Width -= this.ItemImage.Width * .2;
            }
        }

        private void ZoomIn()
        {
            if (this.ItemImage != null)
            {
                this.ItemImage.Height += this.ItemImage.Height * .2;
                this.ItemImage.Width += this.ItemImage.Width * .2;
            }
        }

        private void AddPanelItems()
        {
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
