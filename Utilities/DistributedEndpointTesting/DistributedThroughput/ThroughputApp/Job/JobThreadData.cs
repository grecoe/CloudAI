using ThroughputApp.Configuration;
using ThroughputApp.Job.Utilities;
using ThroughputInterfaces;

namespace ThroughputApp.Job
{

    public class JobThreadData
    {
        public string JobId { get; set; }
        public ThroughputConfiguration Context { get; set; }
        public System.Net.Http.HttpClient Client { get; set; }
        public ThreadRecordSupplier Records { get; set; }
        public OnRecordCompleted RecordComplete { get; set; }
        public OnAllRecordsCompleted ThreadExiting { get; set; }
    }
}
