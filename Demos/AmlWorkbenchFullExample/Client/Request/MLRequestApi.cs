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

using ExperimentClient.ApiData;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ExperimentClient.Request
{
     /// <summary>
     /// Class that makes the request to the service
     /// </summary>
     /// <typeparam name="T">Input type to the service</typeparam>
    class MLRequestApi<T> where T : class, new()
    {
        /// <summary>
        /// Makes the HTTP request to a service
        /// </summary>
        /// <typeparam name="I">Type of input objects</typeparam>
        /// <param name="uri">URI of the service deployed</param>
        /// <param name="key">API Key of the service deployed</param>
        /// <param name="input">The objects to be sent in the request</param>
        /// <returns>A generic object, depending on the service return.</returns>
        public static async Task<I> MakeRequest<I>(String uri, string key, ApiInput<T> input) where I: class, new()
        {
            I returnValue = null;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.BaseAddress = new Uri(uri);

                String content = Newtonsoft.Json.JsonConvert.SerializeObject(input);

                var request = new HttpRequestMessage(HttpMethod.Post, string.Empty);
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response  = client.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    result = result.Replace("\"", String.Empty).Replace("\\", String.Empty);
                    returnValue = Newtonsoft.Json.JsonConvert.DeserializeObject<I>(result);
                }
            }

            return returnValue;
        }

    }
}
