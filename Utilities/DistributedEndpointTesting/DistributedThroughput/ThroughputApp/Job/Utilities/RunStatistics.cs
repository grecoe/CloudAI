using System.Collections.Generic;

namespace ThroughputApp.Job.Utilities
{
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
