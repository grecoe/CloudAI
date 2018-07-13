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
using System.Text;
using Microsoft.ServiceBus.Messaging;
using ThroughputApp.Configuration;
using ThroughputApp.Loggers;

namespace ThroughputApp.Utilities
{
    class ThroughputMessage
    {
        /// <summary>
        /// Client identity to put into a request
        /// </summary>
        public String ClientId { get; set; }
        /// <summary>
        /// Time the record was processed
        /// </summary>
        public DateTime Processed { get; set; }
        /// <summary>
        /// State of the request
        /// 0 - Failure
        /// 1 - Success
        /// 2 - Message
        /// </summary>
        public int State { get; set; }
        /// <summary>
        /// Error or information associated with teh request
        /// </summary>
        public String SendError { get; set; }
        /// <summary>
        /// Time in seconds that AI call took
        /// </summary>
        public float AILatency { get; set; }

        public override string ToString()
        {
            DateTime cleanTime = new DateTime(
                this.Processed.Year,
                this.Processed.Month,
                this.Processed.Day,
                this.Processed.Hour,
                this.Processed.Minute,
                this.Processed.Second,
                0,
                this.Processed.Kind);

            return String.Format("Client,Time,AILatency,State,SendError{0}{1},{2},{3},{4},{5}",
                Environment.NewLine,
                this.ClientId,
                cleanTime.ToUniversalTime().ToString("O"),
                this.AILatency,
                this.State,
                this.SendError);
        }
    }

    class EventHubUtility
    {
        #region Private Members
        /// <summary>
        /// Event hub client object
        /// </summary>
        private static EventHubClient Client { get; set; }
        /// <summary>
        /// Lock to use for threaded access
        /// </summary>
        private static object ThreadLock = new object();

        /// <summary>
        /// Thread data to process
        /// </summary>
        class EventHubThreadData
        {
            public EventHubConfiguration Configuration { get; set; }
            public IEnumerable<ThroughputMessage> Messages { get; set; }
        }
        #endregion

        /// <summary>
        /// Process a single entry. Used mainly for recording messages to the event hub
        /// </summary>
        /// <param name="configuration">Event hub information</param>
        /// <param name="client">Application client name</param>
        /// <param name="state">State of the message</param>
        /// <param name="message">Content of the message</param>
        public static void ProcessOneOff(EventHubConfiguration configuration, String client, int state, String message)
        {
            ThroughputMessage oneOffMessage = new ThroughputMessage()
            {
                ClientId = client,
                Processed = DateTime.UtcNow,
                State = state,
                AILatency = 0,
                SendError = message
            };

            EventHubUtility.ProcessMessages(configuration, new List<ThroughputMessage>() { oneOffMessage });
        }

        /// <summary>
        /// Process a group of messages in a thread.
        /// </summary>
        /// <param name="configuration">Event Hub information</param>
        /// <param name="messages">List of messages to process</param>
        public static void ProcessMessages(EventHubConfiguration configuration, IEnumerable<ThroughputMessage> messages)
        {
            EventHubThreadData data = new EventHubThreadData()
            {
                Configuration = configuration,
                Messages = messages
            };

            System.Threading.ThreadPool.QueueUserWorkItem(UploadMessages, data);
        }

        /// <summary>
        /// Thread routine that uploads a group of messages to the event hub
        /// </summary>
        /// <param name="threadData">Instance of EventHubThreadData</param>
        private static void UploadMessages(object threadData)
        {
            EventHubThreadData data = threadData as EventHubThreadData;

            try
            {
                if (data.Configuration != null &&
                    !String.IsNullOrEmpty(data.Configuration.EventHubName) &&
                    !String.IsNullOrEmpty(data.Configuration.ServiceBusConnectionString))
                {
                    if (EventHubUtility.Client == null)
                    {
                        lock (ThreadLock)
                        {
                            EventHubUtility.Client = EventHubClient.CreateFromConnectionString(
                                data.Configuration.ServiceBusConnectionString,
                                data.Configuration.EventHubName);
                        }
                    }
                    List<EventData> batchList = new List<EventData>();

                    foreach (ThroughputMessage msg in data.Messages)
                    {
                        batchList.Add(new EventData(Encoding.UTF8.GetBytes(msg.ToString())));
                    }

                    lock (EventHubUtility.ThreadLock)
                    {
                        EventHubUtility.Client.SendBatchAsync(batchList);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (EventHubUtility.ThreadLock)
                {
                    if (EventHubUtility.Client != null)
                    {
                        EventHubUtility.Client.Close();
                    }
                    EventHubUtility.Client = null;
                }
            }
        }
    }
}
