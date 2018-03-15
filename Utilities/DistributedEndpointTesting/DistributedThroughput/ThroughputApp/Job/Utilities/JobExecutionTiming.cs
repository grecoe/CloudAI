using System;

namespace ThroughputApp.Job.Utilities
{
    public class JobExecutionTiming
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int TotalRecordsProcessed { get; set; }
    }
}
