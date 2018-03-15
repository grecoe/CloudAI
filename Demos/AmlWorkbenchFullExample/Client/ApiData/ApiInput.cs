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

using System.Collections.Generic;

/// <summary>
/// Using the call az ml service usage realtime -i [serviceid] we get the information we need to construct a 
/// request body to the experiment. Using this output the following class was created to satisfy the request.
/// 
/// Sample CLI command:
///     Usage for cmd: az ml service run realtime -i dangreadysvc.dangreadyclstr2-7f681989.westeurope -d "{\"input_df\": [{\"temp\": 45.9842594460449, \"volt\": 150.513223075022, \"id\": 1.0, \"time\": 1.0, \"rotate\": 277.294013981084}]}"
/// </summary>
namespace ExperimentClient.ApiData
{
    /// <summary>
    /// Generic class for inputs to the service
    /// </summary>
    /// <typeparam name="T">Type of class that make up 
    /// the input types to the service</typeparam>
    class ApiInput<T> where  T : class, new()
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "input_df")]
        public List<T> DataRequest { get; set; }

        public ApiInput()
        {
            this.DataRequest = new List<T>();
        }
    }
}
