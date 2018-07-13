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
using System.Net.Http.Headers;
using ThroughputInterfaces;
using ThroughputApp.Configuration;
using ThroughputApp.Job.Utilities;
using ThroughputInterfaces.Support;

namespace ThroughputApp.Job
{
    /// <summary>
    /// Class that manages all jobs in a run
    /// </summary>
    public class JobManager 
    {
        #region Events
        public event OnRecordCompleted RecordCompleted;
        public event OnStatusUpdate StatusUpdate;
        public event OnAllJobsCompleted AllThreadsCompleted;
        #endregion

        #region Properties
        public IRecordProvider RecordProvider { get; set; }
        public Dictionary<String, JobExecutionTiming> ExecutionData { get; set; }
        public ThroughputConfiguration Context { get; set; }
        #endregion

        #region Private Members
        private ThreadRecordSupplier RecordSupplier { get; set; }
        private Dictionary<String,System.Net.Http.HttpClient> HttpClients { get; set; }
        private List<System.Threading.Thread> Threads { get; set; }

        private static object ThreadLock = new object();
        #endregion

        public JobManager()
        {
            this.ExecutionData = new Dictionary<String, JobExecutionTiming>();
            this.Threads = new List<System.Threading.Thread>();
            this.HttpClients = new Dictionary<string, System.Net.Http.HttpClient>();
        }

        public JobManager(
            IRecordProvider recordProvider,
            ThroughputConfiguration context,
            OnRecordCompleted jobComplete,
            OnStatusUpdate update,
            OnAllJobsCompleted allCompleted)
            :this()
        {
            this.RecordProvider = recordProvider;
            this.Context = context;
            this.RecordCompleted += jobComplete;
            this.StatusUpdate += update;
            this.AllThreadsCompleted += allCompleted;
        }

        public void StartJobs()
        {
            this.RecordSupplier = new ThreadRecordSupplier(this.Context, this.RecordProvider,this.StatusUpdate);
            this.ExecutionData.Clear();
            this.Threads.Clear();

            InternalStartJobs();
        }

        private void InternalStartJobs()
        {
            this.StatusUpdate?.Invoke("Starting jobs, queue ready");

            for (int i = 0; i < this.Context.Execution.ThreadCount; i++)
            {
                string jobId = Guid.NewGuid().ToString("D");

                System.Net.Http.HttpClient client = this.CreateClient(jobId);
                JobThreadData threadData = new JobThreadData()
                {
                    JobId = jobId,
                    Context = this.Context,
                    Client = client,
                    Records = this.RecordSupplier,
                    RecordComplete = this.RecordComplete,
                    ThreadExiting = this.ThreadCompleted
                };

                // Tag the start
                this.ExecutionData.Add(threadData.JobId, new JobExecutionTiming() { Start = DateTime.Now });

                // Create and start the thread
                System.Threading.Thread newThread = new System.Threading.Thread(Job.ScoreRecord);
                newThread.Start(threadData);
                this.Threads.Add(newThread);

                this.StatusUpdate?.Invoke("Starting job : " + jobId);

            }
        }

        #region Handlers
        private void ThreadCompleted(String jobId, int processed, String optionalErrorInfo)
        {
            lock (JobManager.ThreadLock)
            {
                if(this.ExecutionData.ContainsKey(jobId))
                {
                    this.ExecutionData[jobId].End = DateTime.Now;
                    this.ExecutionData[jobId].TotalRecordsProcessed = processed;
                }

                this.RemoveClient(jobId);

                this.StatusUpdate?.Invoke(String.Format("Thread completed - {0}", jobId));
                if (!String.IsNullOrEmpty(optionalErrorInfo))
                {
                    this.StatusUpdate?.Invoke(String.Format("\tError : {0}", optionalErrorInfo));
                }

                if (this.HttpClients.Count == 0)
                {
                    this.AllThreadsCompleted?.Invoke();
                }
            }
        }
        private void RecordComplete(string jobId, int recordId, ScoringExecutionSummary executionTime)
        {
            lock (JobManager.ThreadLock)
            {
                this.RecordCompleted?.Invoke(jobId, recordId, executionTime);
            }
        }
        #endregion

        #region Http Clients
        private void RemoveClient(string jobId)
        {
            lock (JobManager.ThreadLock)
            {
                if (this.HttpClients.ContainsKey(jobId))
                {
                    this.HttpClients[jobId].Dispose();
                    this.HttpClients.Remove(jobId);
                }
            }
        }

        private System.Net.Http.HttpClient CreateClient(string jobId)
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

            if (!String.IsNullOrEmpty(this.RecordProvider.EndpointKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.RecordProvider.EndpointKey);
            }

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.BaseAddress = new Uri(this.RecordProvider.EndpointUrl);
            if (!String.IsNullOrEmpty(this.Context.Execution.ClientName))
            {
                client.DefaultRequestHeaders.Add("User-Agent", this.Context.Execution.ClientName);
            }
            else
            {
                client.DefaultRequestHeaders.Add("User-Agent", "ThroughputApp");
            }

            this.HttpClients.Add(jobId, client);

            return client;
        }
        #endregion
    }
}
