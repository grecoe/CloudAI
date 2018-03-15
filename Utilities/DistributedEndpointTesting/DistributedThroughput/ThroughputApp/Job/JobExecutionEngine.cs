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
            this.Statistics = new RunStatistics();
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
                String.Format("{0},{1},{2},{3},{4},{5},{6},{7}",
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
