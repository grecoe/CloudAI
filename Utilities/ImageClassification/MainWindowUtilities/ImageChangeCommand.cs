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

using System;
using System.Windows.Input;

namespace ImageClassifier.MainWindowUtilities
{
    public delegate void OnNextImage(bool? jump);
    public delegate void OnPrevImage();

    /// <summary>
    /// ICommand objects are required for KeyBinding so we can enable
    /// actions through key commands.
    /// 
    /// This command is the next/previous buttons for browsing
    /// </summary>
    class ImageChangeCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Delegates provided in one of the constructor to call
        /// </summary>
        public OnNextImage OnNext;
        public OnPrevImage OnPrev;
        /// <summary>
        /// Button tied to the action and used for enabling execting change.
        /// </summary>
        public System.Windows.Controls.Button Button { get; set; }

        public ImageChangeCommand(System.Windows.Controls.Button button, OnNextImage nextImage)
        {
            this.Button = button;
            this.OnNext = nextImage;
        }

        public ImageChangeCommand(System.Windows.Controls.Button button, OnPrevImage prev)
        {
            this.Button = button;
            this.OnPrev= prev;
        }

        public bool CanExecute(object parameter)
        {
            this.CanExecuteChanged?.Invoke(null, null);
            if (this.Button != null && this.Button.IsEnabled)
            {
                return true;
            }
            return false;
        }

        public void Execute(object parameter)
        {
            if (this.CanExecute(null))
            {
                this.OnPrev?.Invoke();
                this.OnNext?.Invoke(null);
            }
        }
    }

}
