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
