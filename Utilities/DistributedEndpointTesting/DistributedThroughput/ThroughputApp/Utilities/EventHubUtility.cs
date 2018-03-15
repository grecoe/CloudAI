using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using ThroughputApp.Configuration;

namespace ThroughputApp.Utilities
{
    class ThroughputMessage
    {
        public String ClientId { get; set; }
        public DateTime Processed { get; set; }
        public int State { get; set; }
        public String SendError { get; set; }
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
        private static EventHubClient Client { get; set; }
        private static object ThreadLock = new object();
        class EventHubThreadData
        {
            public EventHubConfiguration Configuration { get; set; }
            public IEnumerable<ThroughputMessage> Messages { get; set; }
        }

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

        public static void ProcessMessages(EventHubConfiguration configuration, IEnumerable<ThroughputMessage> messages)
        {
            EventHubThreadData data = new EventHubThreadData()
            {
                Configuration = configuration,
                Messages = messages
            };

            System.Threading.ThreadPool.QueueUserWorkItem(UploadMessages, data);
        }

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
