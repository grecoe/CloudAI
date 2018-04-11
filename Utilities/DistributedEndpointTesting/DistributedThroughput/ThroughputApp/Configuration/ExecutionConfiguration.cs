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

namespace ThroughputApp.Configuration
{
    public class ExecutionConfiguration
    {
        /// <summary>
        /// Starting thread count
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "threadCount")]
        public int ThreadCount { get; set; }

        /// <summary>
        /// Thread count increment
        /// Set to 0 if only one thread count type is required
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "threadStep")]
        public int ThreadStep { get; set; }

        /// <summary>
        /// Max thread count.
        /// Set to ThreadCount to 
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "maxThreadCount")]
        public int MaxThreadCount { get; set; }

        /// <summary>
        /// How many iterations for the number of records per thread count.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "testCountPerThreadStep")]
        public int TestCountPerThreadStep { get; set; }

        /// <summary>
        /// Number of times the network call should be retried before marking it as failure
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "retryCount")]
        public int RetryCount { get; set; }

        /// <summary>
        /// Name to put in both HTTP requests to service as user_agent adn the client name
        /// to be used in the event hub settings.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "clientName")]
        public string ClientName { get; set; }


        [Newtonsoft.Json.JsonProperty(PropertyName = "autoScaling")]
        public bool AutoScaling { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "autoScaleIncrement")]
        public int AutoScaleIncrement { get; set; }
    }
}
