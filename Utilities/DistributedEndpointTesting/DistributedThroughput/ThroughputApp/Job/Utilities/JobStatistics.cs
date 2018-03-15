using System;
using System.Collections.Generic;
using System.Linq;
using ThroughputInterfaces.Support;

namespace ThroughputApp.Job.Utilities
{
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
