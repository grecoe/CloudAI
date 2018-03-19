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
using System.Windows;

namespace ImageClassifier.Interfaces
{
    /// <summary>
    /// Delegate that handles notifications when the underlying configuration data
    /// has been updated.
    /// </summary>
    public delegate void OnConfigurationUpdatedHandler(IDataSource source);
    /// <summary>
    /// Delegate that handles notifications when the underlying data
    /// catalog has been updated.
    /// </summary>
    public delegate void OnUpdateSourceData(IDataSource source);

    /// <summary>
    /// IConfigurationControl is the UI Element from an IDataSource to configure
    /// itself by allowing a parent to display it.
    /// </summary>
    public interface IConfigurationControl
    {
        /// <summary>
        /// Delegate that the caller can provide to find out when the data 
        /// from the source has been udpated.
        /// </summary>
        OnUpdateSourceData OnSourceDataUpdated { get; set; }
        /// <summary>
        /// Delegate that the caller can provide to find out when the configuration
        /// has been changed.
        /// </summary>
        OnConfigurationUpdatedHandler OnConfigurationUdpated { get; set; }

        /// <summary>
        /// Parent window that is hosting this control
        /// </summary>
        Window Parent { get; set; }

        /// <summary>
        /// Title for the control, used in the TabItem
        /// </summary>
        string Title { get; }

        /// <summary>
        /// The actual control to display
        /// </summary>
        UIElement Control { get; }
    }
}
