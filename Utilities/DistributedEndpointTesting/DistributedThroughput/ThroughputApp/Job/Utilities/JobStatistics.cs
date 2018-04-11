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
using System.Linq;

namespace ThroughputApp.Job.Utilities
{
    /// <summary>
    /// Statistics for a single job
    /// </summary>
    class JobStatistics
    {
        public double TotalExecutionTime { get; set; }
        public double AverageExecutionTime { get; set; }
        public double ShortestExecutionTime { get; set; }
        public double LongestExectuionTime { get; set; }
        public int MaxProcessedRecords { get; set; }
        public int MinProcessedRecords { get; set; }

        public JobStatistics(IEnumerable<JobExecutionTiming> executionData)
        {
            this.ShortestExecutionTime = 1500;

            int jobCount = executionData.Count();
            DateTime earliestTime = DateTime.Now;
            DateTime latestTime = DateTime.Now.AddDays(-1);
            double totalExecutionTime = 0;
            this.MinProcessedRecords = 100000;

            foreach (JobExecutionTiming execData in executionData)
            {
                double executionTime = (execData.End - execData.Start).TotalSeconds;
                totalExecutionTime += executionTime;

                if (execData.TotalRecordsProcessed > this.MaxProcessedRecords)
                {
                    this.MaxProcessedRecords = execData.TotalRecordsProcessed;
                }

                if (execData.TotalRecordsProcessed < this.MinProcessedRecords)
                {
                    this.MinProcessedRecords = execData.TotalRecordsProcessed;
                }

                // Total - earliest start?
                if (execData.Start < earliestTime)
                {
                    earliestTime = execData.Start;
                }

                // Total - latest end ?
                if (execData.End > latestTime)
                {
                    latestTime = execData.End;
                }

                // Is it shortest?
                if (executionTime < this.ShortestExecutionTime)
                {
                    this.ShortestExecutionTime = executionTime;
                }

                // Is it longest?
                if (executionTime > this.LongestExectuionTime)
                {
                    this.LongestExectuionTime = executionTime;
                }
            }

            this.TotalExecutionTime = (latestTime - earliestTime).TotalSeconds;
            this.AverageExecutionTime = totalExecutionTime / jobCount;
        }
    }
}
