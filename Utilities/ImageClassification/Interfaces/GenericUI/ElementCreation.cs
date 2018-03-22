using ImageClassifier.UIUtils;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ImageClassifier.Interfaces.GenericUI
{
    class ElementCreation
    {
        public static Image CreateUiImage(CurrentItem currentSource, double parentWidth, double parentHeight, System.IO.MemoryStream stream)
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


            currentSource.OriginalSize = new System.Drawing.Size((int)bi.Width, (int)bi.Height);
            currentSource.CurrentSize = new System.Drawing.Size((int)bi.Width, (int)bi.Height);

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
                    currentSource.CurrentSize = new System.Drawing.Size((int)newImage.Width, (int)newImage.Height);
                }
            }
            return newImage;
        }
    }
}
