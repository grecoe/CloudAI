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

using ImageClassifier.Interfaces;
using System.Drawing;

namespace ImageClassifier.UIUtils
{
    /// <summary>
    /// Class used by the main window to track what the current image being displayed is.
    /// </summary>
    public class CurrentItem
    {
        /// <summary>
        /// The current IDataSource object that is being displayed
        /// </summary>
        public SourceFile CurrentSource { get; set; }
        /// <summary>
        /// Original Size of the image when loaded from disk
        /// </summary>
        public Size OriginalSize { get; set; }
        /// <summary>
        /// Current size of the image as being shown.
        /// </summary>
        public Size CurrentSize { get; set; }
        /// <summary>
        /// Determine the current zoom level of the image
        /// </summary>
        public int Zoom
        {
            get
            {
                double zoomLevel = ((double)this.CurrentSize.Height / (double)this.OriginalSize.Height)*100;
                double remain = (zoomLevel - (int)zoomLevel);
                int zoom = ((int)zoomLevel + (remain > 0.5 ? 1 : 0)); 
                return zoom;
            }
        }

        public CurrentItem()
        {
            this.CurrentSize = new Size();
            this.OriginalSize = new Size();
            this.CurrentSource = new SourceFile();
        }
    }
}
