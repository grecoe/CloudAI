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
