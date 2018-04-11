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
using ThroughputApp.Configuration;
using ThroughputApp.DefaultProvider;
using ThroughputInterfaces;
using ThroughputInterfaces.Configuration;

namespace ThroughputApp.Job.Utilities
{
    /// <summary>
    /// Class used internally to ThreadRecordSupplier when determining
    /// what items from teh IRecordProvider IDictionary[int, object] to 
    /// use per batch. 
    /// </summary>
    class BatchData
    {
        public int StartItem { get; set; }
        public int EndItem{ get; set; }
        public int BatchSize { get; set; }

    }

    /// <summary>
    /// Class that obtains records from wherever they are located for the jobs
    /// being executed then splits them into evenly distributed batchs.
    /// </summary>
    public class ThreadRecordSupplier
    {
        #region Members
        /// <summary>
        /// Thread safe lock
        /// </summary>
        private static Object ThreadLock = new object();
        /// <summary>
        /// Overall application configuration object
        /// </summary>
        private ThroughputConfiguration Configuration { get; set; }
        /// <summary>
        /// The IRecordProvider used for this test to supply the messages
        /// that are to be sent
        /// </summary>
        private IRecordProvider Provider{ get; set; }
        /// <summary>
        /// Callback function for reporting status back.
        /// </summary>
        private OnStatusUpdate OnStatus;

        #region Batch Information/ Message Storage
        /// <summary>
        /// All records in the recordprovider list start with a 0 based index
        /// and increment by one down the list. This is the last item id that was 
        /// added to a batch.
        /// </summary>
        private static int LastItem { get; set; }
        /// <summary>
        /// The number of batches that have been served up. 
        /// </summary>
        private static int ServedBatches { get; set; }
        /// <summary>
        /// Used on a per run basis. This number is the uneven number of messages 
        /// in a list based the number of threads. i.e. messageCount%threadCount
        /// Then used during distribution of messages to ensure that the thread 
        /// list of messages is not different than off by 1. 
        /// </summary>
        private static int UnevenBatch { get; set; }
        /// <summary>
        /// The messages that are to be sent. Kept static so when the test is re-run 
        /// the messages do not need to be reloaded by the IRecordProvider
        /// </summary>
        private static Dictionary<int, object> Cache = new Dictionary<int, object>();
        #endregion
        #endregion

        public ThreadRecordSupplier(ThroughputConfiguration configuration, 
            IRecordProvider context,
            OnStatusUpdate onStatus = null)
        {
            this.Configuration = configuration;
            this.Provider = context;
            this.OnStatus = onStatus;
            PopulateRecordList();
        }

        /// <summary>
        /// Populates the cache from the IRecordProvider based on the number of messages 
        /// identified in the configuration/context.
        /// </summary>
        private void PopulateRecordList()
        {
            // Determine how many messages are required.
            RecordProviderConfiguration configuration = this.Configuration.RecordConfiguration;
            if (this.Provider.GetType().IsAssignableFrom(typeof(DefaultRecordProvider)))
            {
                configuration = new RecordProviderConfiguration()
                {
                    RecordCount = this.Configuration.DefaultProvider.RecordCount
                };
            }

            // Reset counters for GetNextBatch() and populate the cache if the record count
            // differs from the cache count.
            ThreadRecordSupplier.LastItem = 0;
            ThreadRecordSupplier.ServedBatches = 0;
            ThreadRecordSupplier.UnevenBatch = 0;
            if (ThreadRecordSupplier.Cache.Count != configuration.RecordCount)
            {
                ThreadRecordSupplier.Cache = new Dictionary<int, object>(this.Provider.LoadRecords(configuration, this.OnStatus));
            }
        }

        private BatchData GetBatchData()
        {
            BatchData returnData = new BatchData();

            // If this is the first batch served, figure out the odd messages
            // so we can smooth out the distribution to all threads
            if (ThreadRecordSupplier.ServedBatches == 1)
            {
                double fbatchSize = ThreadRecordSupplier.Cache.Count / (double)this.Configuration.Execution.ThreadCount;
                ThreadRecordSupplier.UnevenBatch = (int)((fbatchSize - (int)fbatchSize) * this.Configuration.Execution.ThreadCount);
            }

            // All figure out the batch and if any uneven messages left
            // pick them up one at a time
            returnData.BatchSize = ThreadRecordSupplier.Cache.Count / this.Configuration.Execution.ThreadCount;
            if (ThreadRecordSupplier.UnevenBatch > 0)
            {
                returnData.BatchSize += 1;
                ThreadRecordSupplier.UnevenBatch -= 1;
            }

            // Get the start and end number and record the last number
            // we are getting. The list of messages ALWAYS 0 based index
            returnData.StartItem = (ThreadRecordSupplier.LastItem == 0 ? 0 : ThreadRecordSupplier.LastItem);
            returnData.EndItem = returnData.StartItem + returnData.BatchSize - 1;
            ThreadRecordSupplier.LastItem = returnData.EndItem + 1;

            // If we are past the end, truncate, if we don't have enough for another
            // of this is the last batch, grab whatever is left in the array.
            if (returnData.EndItem > ThreadRecordSupplier.Cache.Count ||
                (ThreadRecordSupplier.Cache.Count - returnData.EndItem) < returnData.BatchSize ||
                ThreadRecordSupplier.ServedBatches == this.Configuration.Execution.ThreadCount)
            {
                returnData.EndItem = ThreadRecordSupplier.Cache.Count;
            }
            return returnData;
        }

        public Dictionary<int,String> GetNextBatch()
        {
            Dictionary<int, string> reuturnDictionary = new Dictionary<int, string>();
            lock(ThreadRecordSupplier.ThreadLock)
            {
                ThreadRecordSupplier.ServedBatches++;

                BatchData data = GetBatchData();
 
                Dictionary<int, object> tmpDict = ThreadRecordSupplier.Cache
                    .Where(k => k.Key >= data.StartItem && k.Key <= data.EndItem).ToDictionary(kv => kv.Key, kv => kv.Value);

                // Translate them all to usable input (JSON) for the endpoint.
                foreach (KeyValuePair<int, object> kvpo in tmpDict)
                {
                    String recordData = String.Empty;
                    if (kvpo.Value is String)
                    {
                        recordData = kvpo.Value.ToString();
                    }
                    else
                    {
                        recordData = Newtonsoft.Json.JsonConvert.SerializeObject(kvpo.Value, Newtonsoft.Json.Formatting.Indented);
                    }
                    reuturnDictionary.Add(kvpo.Key, recordData);
                }
            }
            return reuturnDictionary;
        }



    }
}
