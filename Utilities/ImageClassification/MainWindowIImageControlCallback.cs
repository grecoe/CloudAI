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
using ImageClassifier.Interfaces.GenericUI;
using System;

namespace ImageClassifier
{
    public partial class MainWindow
    {
        /// <summary>
        /// Called from the ISingleImageControl control when the displayed image changes
        /// </summary>
        /// <param name="file">File on display</param>
        private void ISingleImageControlFileChanged(SourceFile file)
        {
            if (this.SelectedDataSource != null)
            {
                if (file != null)
                {
                    ClassificationCheckboxPanelHelper.MakeSelection(
                        this.ClassificationTabSelectionPanel,
                        file);
                }

                this.StatusBarLocationStatus.Text =
                    String.Format("Viewing {0} of {1} ",
                    this.SelectedDataSource.CurrentContainerIndex + 1,
                    this.SelectedDataSource.CurrentContainerCollectionCount);
            }
        }

        /// <summary>
        /// Called from the IMultiImageControl control when the group of images changes.
        /// </summary>
        /// <param name="file">Unused as IMultiImageControl uses batches of files.</param>
        private void IMultiImageControlGroupChanged(SourceFile file)
        {
            if (this.SelectedDataSource != null && !String.IsNullOrEmpty(this.SelectedDataSource.CurrentContainer))
            {
                String classification = (this.SelectedDataSource as IMultiImageDataSource).CurrentContainerAsClassification;
                int groupSize = (this.SelectedDataSource as IMultiImageDataSource).BatchSize;

                int currentIndex = this.SelectedDataSource.CurrentContainerIndex+1;
                int totalGroups = this.SelectedDataSource.CurrentContainerCollectionCount / groupSize;
                int currentGroup = (currentIndex > 0) ? (int)Math.Round((decimal)currentIndex / (decimal)groupSize) : 1;

                if(totalGroups == 0 && this.SelectedDataSource.CurrentContainerCollectionCount > 0)
                {
                    totalGroups = 1;
                }

                ClassificationCheckboxPanelHelper.MakeSelection(
                    this.ClassificationTabSelectionPanel,
                    classification);

                this.StatusBarLocationStatus.Text =
                    String.Format("Viewing Group :  {0} of {1}",
                    (currentGroup > 0)?currentGroup : 1,
                    totalGroups);
            }
        }

    }
}
