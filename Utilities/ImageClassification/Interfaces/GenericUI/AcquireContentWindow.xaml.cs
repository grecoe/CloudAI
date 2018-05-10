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
using System;
using System.Threading;
using System.Windows;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// Used as an update window while a data source is collecting catalog information
    /// from it's backend source.
    /// </summary>
    public partial class AcquireContentWindow : Window
    {
        #region Private Members
        private ManualResetEvent Cancel { get; set; }
        #endregion

        #region Public Properties
        public event OnLongRunningProcessCompleteHandler JobCompleted;

        public String DisplayContent
        {
            get { return this.txtContent.Text; }
            set
            {
               this.txtContent.Dispatcher.Invoke(() =>
               {
                   this.txtContent.Text = value;
               }, 
               System.Windows.Threading.DispatcherPriority.Render);
            }
        }
        #endregion

        public AcquireContentWindow(Window parent, bool showCancel)
        {
            InitializeComponent();

            this.Cancel = new ManualResetEvent(false);
            this.ButtonCancel.Click += (o, e) => this.CancelTransaction();

            if (parent != null)
            {
                this.Top = parent.Top + (parent.Height - this.Height) / 2;
                this.Left = parent.Left + (parent.Width - this.Width) / 2;
            }

            if (!showCancel)
            {
                this.ButtonCancel.Visibility = Visibility.Collapsed;
            }
        }

        public void StartLongRunningPRocess(Action<ManualResetEvent, Action<string>> action)
        {
            LongRunningProcessData threadData = new LongRunningProcessData()
            {
                Event = this.Cancel,
                Work = action,
                OnStatusUpdate = this.StatusUpdate
            };

            this.Show();
            System.Threading.ThreadPool.QueueUserWorkItem(this.ThreadRoutine, threadData);
        }

        #region Private Helpers
        private void CancelTransaction()
        {
            this.Cancel.Set();
        }

        private void StatusUpdate(string message)
        {
            this.DisplayContent = message;
        }

        private void SafeClose()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Close();
            });
        }

        private void ThreadRoutine(object obj)
        {
            if (obj is LongRunningProcessData)
            {
                LongRunningProcessData data = obj as LongRunningProcessData;
                data.OnStatusUpdate?.Invoke("Starting job");

                data.Work(data.Event, data.OnStatusUpdate);

                data.OnStatusUpdate?.Invoke("Completing job");

                this.JobCompleted?.Invoke();
            }

            this.SafeClose();
        }

        #endregion
    }
}
