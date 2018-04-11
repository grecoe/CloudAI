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
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ThroughputApp.Request
{
    /// <summary>
    /// Response object from ML request
    /// </summary>
    class RequestResult
    {
        /// <summary>
        /// Boolean value of status of call. True means call succeeded
        /// </summary>
        public bool State { get; set; }
        /// <summary>
        /// Number of recorded attempts. 
        /// </summary>
        public int Attempts { get; set; }
        /// <summary>
        /// Http Response Code
        /// </summary>
        public int ResponseCode { get; set; }
        /// <summary>
        /// Response payload
        /// </summary>
        public String Response { get; set; }
        /// <summary>
        /// Time in which the response took
        /// </summary>
        public TimeSpan ResponseTime { get; set; }
    }

    /// <summary>
    /// Class that will call the endpoint, collect the response, time the POST call only 
    /// </summary>
    class EndpointRequest
    {
        public static async Task<RequestResult> MakeRequest(HttpClient client, string content, int retryCount = 1)
        {
            RequestResult returnValue = new RequestResult();

            var request = new HttpRequestMessage(HttpMethod.Post, string.Empty);
            request.Content = new StringContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Make the HTTP call, if it fails, keep retrying until retryCount is hit
            DateTime start = DateTime.Now;
            returnValue.Attempts = 1;
            HttpResponseMessage response = client.SendAsync(request).Result;
            while(!response.IsSuccessStatusCode && returnValue.Attempts < retryCount)
            {
                // Give it a few clicks to see if we are in trouble
                System.Threading.Thread.Sleep(10);

                // Can't send same request twice, so build another....
                request = new HttpRequestMessage(HttpMethod.Post, string.Empty);
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                returnValue.Attempts++;
                response = client.SendAsync(request).Result;
            }
            returnValue.ResponseTime = (DateTime.Now - start);
            returnValue.ResponseCode = (int)response.StatusCode;

            // Report the status after any and all retries have occured.
            if (response.IsSuccessStatusCode)
            {
                returnValue.State = true;
                returnValue.Response = await response.Content.ReadAsStringAsync();
            }
            else
            {
                returnValue.State = false;

                returnValue.Response = response.StatusCode.ToString();
                int code = (int)response.StatusCode;

                String strContent = await response.Content.ReadAsStringAsync();
                if (!strContent.Contains("<head>"))
                {
                    returnValue.Response = String.Format("{0} ({2}) - {1}", returnValue.Response, strContent.Trim(), code);
                }

                returnValue.Response = returnValue.Response.Replace('\x0A', '|');
                returnValue.Response = returnValue.Response.Replace(',', '_');
            }

            return returnValue;
        }
    }
}
