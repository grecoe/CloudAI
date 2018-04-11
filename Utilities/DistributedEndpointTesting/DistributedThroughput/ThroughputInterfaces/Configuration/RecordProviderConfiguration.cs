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

namespace ThroughputInterfaces.Configuration
{
    public class RecordProviderConfiguration
    {
        /// <summary>
        /// Execution type can be 
        ///     storage: The RecordProviderStorage contains the information about a storage account
        ///              in which to locate records to use for the engine.
        ///     local : The RecordProviderLocal contains the information about a single local file to be iterated
        ///             on during execution
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "executionType")]
        public String ExecutionType { get; set; }

        /// <summary>
        /// The number of records that will be returned by the interface to iterate over during testing.
        /// This number of records will be sent over the configured number of threads from the main application.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "recordCount")]
        public int RecordCount { get; set; }

        /// <summary>
        /// Azure blob storage account information 
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "storage")]
        public RecordProviderStorage Storage { get; set; }

        /// <summary>
        /// Local file path
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "local")]
        public RecordProviderLocal Local { get; set; }

        public RecordProviderConfiguration()
        {
            this.Storage = new RecordProviderStorage();
            this.Local = new RecordProviderLocal();
        }
    }
}
