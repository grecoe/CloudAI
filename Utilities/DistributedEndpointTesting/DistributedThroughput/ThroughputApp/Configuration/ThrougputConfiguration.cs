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
using ThroughputInterfaces.Configuration;

namespace ThroughputApp.Configuration
{
    public class ThroughputConfiguration
    {
        private const String ConfigurationFile = "ThroughputConfiguration.json";

        [Newtonsoft.Json.JsonIgnore]
        public int SelectedRecordCount { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "recordProviderDiskLocation")]
        public String RecordProviderDiskLocation { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "logDirectory")]
        public String LogDirectory { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "recordProvider")]
        public RecordProviderConfiguration RecordConfiguration { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "execution")]
        public ExecutionConfiguration Execution { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "defaultProvider")]
        public DefaultProviderConfiguration DefaultProvider { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "eventHubConfiguration")]
        public EventHubConfiguration HubConfiguration { get; set; }
        
        protected ThroughputConfiguration()
        {
            this.RecordConfiguration = new RecordProviderConfiguration();
            this.Execution = new ExecutionConfiguration();
            this.DefaultProvider = new DefaultProviderConfiguration();
        }

        public static ThroughputConfiguration LoadConfiguration()
        {
            String path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ThroughputConfiguration.ConfigurationFile);
            String content = System.IO.File.ReadAllText(path);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<ThroughputConfiguration>(content);
        }
    }
}
