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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace ImageClassifier.Interfaces
{
    /// <summary>
    /// Notification event for when the image in the image control changes.
    /// </summary>
    /// <param name="file"></param>
    public delegate void OnImageChanged(SourceFile file);

    /// <summary>
    /// Notification event for when the classifications change.
    /// </summary>
    /// <param name="classifications"></param>
    public delegate void OnClassificationsChanged(List<string> classifications);

    public interface IImageControl
    {
        /// <summary>
        /// Implementation of the OnImageChanged delegate
        /// </summary>
        event OnImageChanged ImageChanged;
        /// <summary>
        /// Parent control for sizing information
        /// </summary>
        UIElement ParentControl { get; set; }
        /// <summary>
        /// KeyBindings for the control to be added to the general key bindings
        /// for the main applicaiton.
        /// </summary>
        IEnumerable<KeyBinding> Bindings { get; }
        /// <summary>
        /// Allow owner to update the classifications on the current image or set of images
        /// </summary>
        void UpdateClassifications(List<string> classifications);
        /// <summary>
        /// DataSource that provides information
        /// </summary>
        IDataSource DataSource { get; }
        /// <summary>
        /// The actual control to display
        /// </summary>
        UIElement Control { get; }
        /// <summary>
        /// Clear UI elements
        /// </summary>
        void Clear();
        /// <summary>
        /// Fast forward through the collection to arrive at
        /// the first un-tagged item in the container collection
        /// </summary>
        void FastForward();
        /// <summary>
        /// Force the UI to show whatever the datasource has
        /// </summary>
        void ShowNext();
    }
}
