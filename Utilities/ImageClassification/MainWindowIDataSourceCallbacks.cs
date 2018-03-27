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
using System;
using System.Collections.Generic;

namespace ImageClassifier
{
    public partial class MainWindow
    {
        #region Data source provider callbacks
        /// <summary>
        /// Called by a data source configuration UI to notify that the 
        /// settings have changed.
        /// </summary>
        /// <param name="sender">The provider that made the change</param>
        private void IDataSourceOnConfigurationUdpated(IDataSource sender)
        {
            // Something changed, make sure we get the annotations saved out
            this.ConfigurationContext.Classifications =
                new List<string>(this.ConfigurationTabTextAnnotationTags.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            this.ConfigurationContext.Save();

            this.PopulateAnnotationsTabAnnotationsPanel();

            // Make sure to capture any changes to the classifications
            if (this.SelectedDataSource != null)
            {
                InitializeUi(false);
            }
        }

        /// <summary>
        /// Called by a data source configuration UI to notify that the 
        /// data for the source has changed.
        /// </summary>
        /// <param name="sender">The provider that made the change</param>
        private void IDataSourceOnSourceDataUpdated(IDataSource sender)
        {
            if (sender == this.SelectedDataSource)
            {
                InitializeUi(true);
            }
        }
        #endregion

    }
}
