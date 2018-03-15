using System;
using ThroughputInterfaces.Support;

namespace ThroughputInterfaces
{
    /// <summary>
    /// Optional delegate to LoadRecords to report back to the calling application what is happening
    /// during the load.
    /// </summary>
    /// <param name="status">Any text you want to show/log</param>
    public delegate void OnStatusUpdate(String status);

    public delegate void OnAllJobsCompleted();

    public delegate void OnRecordCompleted(String jobId, int recordId, ScoringExecutionSummary executionTime);

    public delegate void OnAllRecordsCompleted(String jobId, int processed, string optionalErrorData);

}
