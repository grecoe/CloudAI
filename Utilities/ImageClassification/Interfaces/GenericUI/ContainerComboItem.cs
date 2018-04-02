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

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// UI ComboBox item for source containers.
    /// </summary>
    class ContainerComboItem
    {
        /// <summary>
        /// The name of the source container
        /// </summary>
        public String SourceContainer { get; set; }
        /// <summary>
        /// The source type so that the override ToString() can
        /// return the appropriate answer.
        /// </summary>
        public DataSourceType SourceType { get; set; }

        public ContainerComboItem(DataSourceType type, string file)
        {
            this.SourceType = type;
            this.SourceContainer = file;
        }
        
        public override string ToString()
        {
            String returnValue = String.Empty;
            if (!String.IsNullOrEmpty(this.SourceContainer))
            {
                if (this.SourceType == DataSourceType.Disk)
                {
                    // Directories - show whole thing in this case so you know lineage
                    string root = System.IO.Path.GetPathRoot(this.SourceContainer);
                    returnValue = this.SourceContainer.Substring(root.Length);
                }
                else if(this.SourceType == DataSourceType.LabelledBlob)
                {
                    // Blob prefixes from storage 
                    returnValue = this.SourceContainer.Trim(new char[] { '/', '\\' });
                }
                else if (this.SourceType == DataSourceType.LabelledDisk)
                {
                    // Prefixes from disk (directory)
                    returnValue = this.SourceContainer.Trim(new char[] { '/', '\\' });
                    int idx = returnValue.LastIndexOf('\\');
                    if(idx != -1)
                    {
                        returnValue = returnValue.Substring(idx + 1);
                    }
                }
                else
                {
                    // Files
                    returnValue = System.IO.Path.GetFileName(this.SourceContainer);
                }
            }
            return returnValue; 
        }
    }
}
