using System;
using System.Windows;
using System.Windows.Controls;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ImageControlNotificationDisplay : UserControl
    {
        public ImageControlNotificationDisplay(String message, FrameworkElement parent)
        {
            InitializeComponent();

            if (!String.IsNullOrEmpty(message))
            {
                this.PlaceHolderText.Text = message;
            }

            if(parent != null)
            {
                this.PlaceHolderText.Width = parent.ActualWidth * .8;
            }
        }
    }
}
