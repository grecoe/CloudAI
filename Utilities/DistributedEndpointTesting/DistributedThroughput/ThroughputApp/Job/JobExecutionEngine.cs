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
using ThroughputApp.Configuration;
using ThroughputApp.Job.Utilities;
using ThroughputApp.Loggers;
using ThroughputApp.Utilities;
using ThroughputInterfaces;
using ThroughputInterfaces.Support;

namespace ThroughputApp.Job
{
    /// <summary>
    /// Execution engine that manages all aspects of the execution
    /// </summary>
    class JobExecutionEngine 
    {
        #region Constant Values
        const string SUCCESS = "success";
        const string FAILURE = "failure";
        #endregion

        #region Events
        public event OnRecordCompleted RecordCompleted;
        public event OnAllJobsCompleted AllThreadsCompleted;
        public event OnStatusUpdate StatusUpdate;
        #endregion

        #region Properties
        public ThroughputConfiguration Context { get; set ; }
        public JobManager Manager { get; set; }
        #endregion

        #region Private Members
        private Dictionary<string,int> ErrorTrackingAutoScale { get; set; }

        private RunStatistics Statistics { get; set; }
        #endregion

        public JobExecutionEngine()
        {
            // Key = last success
            // Value = First Error
            this.ErrorTrackingAutoScale = new Dictionary<string, int>();
            this.ErrorTrackingAutoScale[SUCCESS] = -1;
            this.ErrorTrackingAutoScale[FAILURE] = -1;
        }

        public void StartExecution()
        {
            if(this.Statistics == null)
            {
                this.Statistics = new RunStatistics();
            }

            this.Statistics.ResetStatistics();
            this.Statistics.RecordsInRun = this.Context.SelectedRecordCount;

            this.Manager.AllThreadsCompleted += ManagerAllThreadsCompleted;
            this.Manager.RecordCompleted += ManagerRecordCompleted;
            this.Manager.StartJobs();
        }

        #region Callbacks from manager
        private void ManagerRecordCompleted(string jobId, int recordId, ScoringExecutionSummary runTime)
        {
            String output = String.Format("{0},{1},{2},{3},{4},{5},{6}",
                recordId,
                runTime.PayloadSize.ToString(),
                (runTime.State == false ? "0" : "1"),
                runTime.Attempts,
                runTime.Response,
                runTime.ExecutionTime.TotalSeconds.ToString(),
                runTime.AITime.TotalSeconds.ToString());

            this.Statistics.AITimes.Add(runTime.AITime.TotalSeconds);
            this.Statistics.FailureCount += (runTime.State == false ? 1 : 0);

            TimingLog.RecordTiming(this.Context, output);
        }

        private void ManagerAllThreadsCompleted()
        {
            this.Manager.AllThreadsCompleted -= ManagerAllThreadsCompleted;
            this.Manager.RecordCompleted -= ManagerRecordCompleted;

            JobStatistics stats = new JobStatistics(this.Manager.ExecutionData.Values);

            bool hasErrors = (this.Statistics.FailureCount > 0);

            HistoryLog.RecordHistory(
                this.Context,
                String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                    this.Statistics.ExecutionCount,
                    this.Context.Execution.TestCountPerThreadStep,
                    this.Context.Execution.ThreadCount,
                    this.Statistics.RecordsInRun,
                    this.Statistics.FailureCount,
                    this.Statistics.AverageAITime(),
                    stats.TotalExecutionTime,
                    (this.Statistics.RecordsInRun / stats.TotalExecutionTime),
                    stats.MaxProcessedRecords,
                    stats.MinProcessedRecords)
                );

            this.StatusUpdate?.Invoke(String.Format("Finished job with {0} threads", this.Context.Execution.ThreadCount));

            // Reset AI times and failure counts for next run
            this.Statistics.AITimes.Clear();
            this.Statistics.FailureCount = 0;

            if (this.Context.Execution.AutoScaling)
            {
                if (++this.Statistics.ExecutionCount < this.Context.Execution.TestCountPerThreadStep)
                {
                    String errorText = String.Format("INTERNAL - Scaling up but no more threads left -> {0}.", this.Context.Execution.ThreadCount);

                    if (!hasErrors)
                    {
                        this.ErrorTrackingAutoScale[SUCCESS] = this.Context.Execution.ThreadCount;

                        if(this.ErrorTrackingAutoScale[FAILURE] != -1)
                        {
                            errorText = String.Format("INTERNAL - Previous errors detected, not moving up thread count.");
                        }
                        else if (this.Context.Execution.ThreadCount < this.Context.Execution.MaxThreadCount)
                        {
                            this.Context.Execution.ThreadCount += this.Context.Execution.AutoScaleIncrement;
                            errorText = String.Format("INTERNAL - Scaling up thread count");
                            ClientScalingLog.RecordScalingChange(this.Context, this.Context.Execution.ThreadCount);
                        }
                    }
                    else // Something is wrong, scale back 
                    {
                        this.StatusUpdate?.Invoke("Errors detected, scaling back");
                        this.ErrorTrackingAutoScale[FAILURE] = this.Context.Execution.ThreadCount;

                        errorText = String.Format("INTERNAL - Scaled back to a single thread already");
                        if (this.Context.Execution.ThreadCount > 1)
                        {
                            this.Context.Execution.ThreadCount -= 1;
                            errorText = String.Format("INTERNAL - Scaling back thread count with errors");
                        }
                        ClientScalingLog.RecordScalingChange(this.Context, this.Context.Execution.ThreadCount);
                    }
                    EventHubUtility.ProcessOneOff(this.Context.HubConfiguration,
                        this.Context.Execution.ClientName,
                        2,
                        errorText);

                    this.StartExecution();
                }
                else
                {
                    // Let caller know we're done.
                    this.AllThreadsCompleted?.Invoke();
                }

            }
            else
            {
                if (++this.Statistics.ExecutionCount < this.Context.Execution.TestCountPerThreadStep)
                {
                    this.StartExecution();
                }
                else
                {
                    // Increase thread count until max
                    if (this.Context.Execution.ThreadStep > 0 &&
                        this.Context.Execution.ThreadCount + this.Context.Execution.ThreadStep <= this.Context.Execution.MaxThreadCount)
                    {
                        this.Statistics.ExecutionCount = 0;
                        this.Context.Execution.ThreadCount += this.Context.Execution.ThreadStep;
                        this.StartExecution();
                    }
                    else
                    {
                        this.AllThreadsCompleted?.Invoke();
                    }
                }
            }
        }
        #endregion
    }
}
