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

namespace ThroughputApp.Job.Utilities
{
    /// <summary>
    /// Statistics for a complete run 
    /// </summary>
    public class RunStatistics
    {
        /// <summary>
        /// A list of all AI times, time from just before HttpPOST to just after to track 
        /// the latency only of the endpoing call.
        /// </summary>
        public List<double> AITimes { get; set; }
        /// <summary>
        /// Tracker for each time a run is completed to know when to bump the thread count
        /// </summary>
        public int ExecutionCount { get; set; }
        /// <summary>
        /// Failure count per iteration
        /// </summary>
        public int FailureCount { get; set; }
        /// <summary>
        /// How many records are in the run.
        /// </summary>
        public int RecordsInRun { get; set; }

        public RunStatistics()
        {
            this.AITimes = new List<double>();
        }

        public double AverageAITime()
        {
            double total = 0.0;
            foreach (double tm in this.AITimes)
            {
                total += tm;
            }

            return total / (double)this.AITimes.Count;
        }
    }
}
