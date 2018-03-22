using ImageClassifier.Interfaces.GlobalUtils;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// The grid element in the multiimagecontrol
    /// </summary>
    public partial class MultiImageInstance : UserControl
    {
        private CurrentItem Item { get; set; }
        private Image ItemImage { get; set; }
        private double ParentHeight { get; set; }
        private double ParentWidth { get; set; }

        public MultiImageInstance(CurrentItem item, double parentHeight, double parentWidth)
        {
            InitializeComponent();

            this.Item = item;
            this.ParentHeight = ( parentHeight > 0) ? parentHeight : 300;
            this.ParentWidth = (parentWidth > 0) ? parentWidth : 300; ;


            this.ImageName.Text = item.CurrentSource.Name;
            this.ImageLabels.Text = String.Join(",", item.CurrentSource.Classifications);

            this.ImagePanel.MouseDown += ImagePanel_MouseDown;
            System.Threading.Thread startThread = new System.Threading.Thread(ThreadCollectImage);
            startThread.SetApartmentState(System.Threading.ApartmentState.STA);
            startThread.Start(null);
        }

        private void ImagePanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ButtonState == MouseButtonState.Pressed)
            {
                MultiInstanceDetailWindow window = new MultiInstanceDetailWindow(this.Item, this.ItemImage);
                window.ShowDialog();
            }
        }

        private void ThreadCollectImage(object unused)
        {
            // Give up 30% of the height to the text boxes
            double height = this.ParentHeight * .7;
            double width = this.ParentWidth * .7;

            this.ImagePanel.Dispatcher.Invoke(() => {
                System.IO.MemoryStream downloadFile = FileUtils.GetFileStream(this.Item.CurrentSource.DiskLocation);
                this.ItemImage = ElementCreation.CreateUiImage(this.Item, height, width, downloadFile);
                this.ImagePanel.Children.Add(this.ItemImage);
            });

            this.ImageSize.Dispatcher.Invoke(() => {
                this.ImageSize.Text = String.Format("{0}w x {1}h", this.Item.OriginalSize.Width, this.Item.OriginalSize.Height);
            });
        }
    }
}
