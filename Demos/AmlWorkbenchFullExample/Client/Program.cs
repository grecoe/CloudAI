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
using ExperimentClient.Request;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExperimentClient
{
    class Program
    {
        /// <summary>
        /// The following two fields are obtained after the service has been deployed to the Kubernetes cluster. This information 
        /// was gathered from the site:
        /// 
        /// https://docs.microsoft.com/en-us/azure/machine-learning/preview/model-management-consumption
        /// 
        /// Using the Azure ML CLI on the Windows DSVM from the experiment run the following commands:
        /// 
        /// 1. az ml service list realtime -o table
        /// 2. Copy the service ID for your service
        /// 3. az ml service usage realtime -i [YOUR_SERVICE_ID]
        ///     Here you will find the URI of the service
        /// 4. az ml service keys realtime -i [YOUR_SERVICE_ID]
        ///     Here you will get the service key
        /// </summary>
        const String APIKey = "[YOUR_API_KEY]";
        const String APIUri = "[YOUR_API_URI]";
        private static List<Double> Results { get; set; }

        static void Main(string[] args)
        {
            ApiInput<ManufacturingInput> input = new ApiInput<ManufacturingInput>();

            input.DataRequest.Add(new ManufacturingInput()
            {
                DeviceId = 1,
                OperatingVoltage = 241.50,
                OperationTemperature = 189.2342,
                Rotation = 120,
                TimeStamp = 3,
            });

            ExecuteExpermimentEndpoint(input).Wait();

            foreach(double d in Results)
            {
                Console.WriteLine("Result : {0}", d);
            }
            Console.ReadLine();
        }

        /// <summary>
        /// Async call to hit the actual endpoint.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Data to be sent to the experiment endpoint.</returns>
        private static async Task ExecuteExpermimentEndpoint(ApiInput<ManufacturingInput> input)
        {
            foreach(ManufacturingInput minput in input.DataRequest)
            {
                Console.WriteLine("Device {0} in payload", minput.DeviceId);
            }
            Program.Results = await MLRequestApi<ManufacturingInput>.MakeRequest<List<Double>>(APIUri, APIKey, input);
        }
    }
}
