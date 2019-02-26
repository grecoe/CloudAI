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
using System.Collections.Generic;
using Newtonsoft.Json;

namespace StorageConsolidation.Config
{
    /// <summary>
    /// Configuration has the connection strings for 
    /// 
    /// 1. Destination Storage Account connection strings
    /// 2. List of source Azure Storage Account connection strings
    /// 
    /// To get a connection string
    ///     1. Go to portal.azure.com
    ///     2. Find the storage account you want to be either the destination or source.
    ///     3. Click on the storage account
    ///     4. Click on Access keys 
    ///     5. Copy teh value of key1/Connection string
    /// </summary>
    class Configuration
    {
        #region Non-JSON Related
        [JsonIgnore]
        private const String CONFIG_FILE = "Configuration.json";
        #endregion

        #region Properties
        [JsonProperty(PropertyName = "Destination")]
        public String Destination { get; set; }

        [JsonProperty(PropertyName = "Sources")]
        public List<String> Sources { get; set; }
        #endregion

        protected Configuration()
        {
            this.Sources = new List<string>();
        }

        public static Configuration LoadConfiguration()
        {
            String path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
            String content = System.IO.File.ReadAllText(path);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(content);
        }

   
    }
}
