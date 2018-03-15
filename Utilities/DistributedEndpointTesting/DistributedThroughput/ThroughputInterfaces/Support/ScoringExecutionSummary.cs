using System;

namespace ThroughputInterfaces.Support
{
    public class ScoringExecutionSummary
    {
        /// <summary>
        /// Time it took to complete the entire transaction 
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
        /// <summary>
        /// Time in entire transaction dedicated to making the endpoint call
        /// </summary>
        public TimeSpan AITime { get; set; }
        /// <summary>
        /// Size of the payload that was sent
        /// </summary>
        public int PayloadSize { get; set; }
        /// <summary>
        /// HTTPResponse payload
        /// </summary>
        public String Response { get; set; }
        /// <summary>
        /// Indicates the state of the request, true is successful
        /// </summary>
        public bool State { get; set; }
        /// <summary>
        /// Number of attempts
        /// </summary>
        public int Attempts { get; set; }
    }
}
