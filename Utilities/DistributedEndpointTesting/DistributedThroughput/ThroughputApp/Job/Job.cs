using System;
using System.Collections.Generic;
using ThroughputApp.Request;
using ThroughputApp.Utilities;
using ThroughputInterfaces.Support;

namespace ThroughputApp.Job
{
    class Job
    {
        public static async void ScoreRecord(object data)
        {
            if (data is JobThreadData)
            {
                JobThreadData threadData = data as JobThreadData;
                List<ThroughputMessage> eventMessages = new List<ThroughputMessage>();

                int recordsProcessed = 0;

                // Gets a list of messages appropriate to the number of threads that are
                // being run for this test.
                Dictionary<int, string> rvp = threadData.Records.GetNextBatch();

                String optionalError = String.Empty;
                try
                {
                    // Consecutive errors. This is when an attempt to send fails
                    // the limit and an error is returned here. If we get three
                    // of these in a row, kill the thread. 
                    List<String> consecutiveErrorCodes = new List<String>();
                    foreach (KeyValuePair<int, string> record in rvp)
                    {
                        recordsProcessed++;

                        // Full Execution Time
                        DateTime fullStart = DateTime.Now;

                        ScoringExecutionSummary jobExecutionSummary = new ScoringExecutionSummary();
                        jobExecutionSummary.PayloadSize = record.Value.Length;

                        // Get new time for AI call
                        RequestResult returnValue = await EndpointRequest.MakeRequest(
                                                    threadData.Client,
                                                   record.Value,
                                                   threadData.Context.Execution.RetryCount);

                        // Record timing for request
                        jobExecutionSummary.AITime = returnValue.ResponseTime;
                        jobExecutionSummary.Response = returnValue.Response;
                        jobExecutionSummary.State = returnValue.State;
                        jobExecutionSummary.Attempts = returnValue.Attempts;

                        jobExecutionSummary.ExecutionTime = (DateTime.Now - fullStart);

                        // Let the caller know about the call, good or bad
                        threadData.RecordComplete?.Invoke(threadData.JobId, record.Key, jobExecutionSummary);

                        // Record this send data for the event hub
                        RecordEventHubRecords(threadData.Context.Execution.ClientName, eventMessages, returnValue);

                        if(returnValue.State == false)
                        {
                            consecutiveErrorCodes.Add(returnValue.ResponseCode.ToString());
                            if(consecutiveErrorCodes.Count > 3)
                            {
                                String errorText = 
                                    String.Format("Too many consecutive errors ; {0}",
                                    String.Join(" | ", consecutiveErrorCodes));

                                EventHubUtility.ProcessOneOff(threadData.Context.HubConfiguration,
                                            threadData.Context.Execution.ClientName,
                                            2,
                                            errorText);

                                // Get this thread to terminate and report
                                throw new Exception(errorText);
                            }
                        }
                        else
                        {
                            consecutiveErrorCodes.Clear();
                        }
                    }
                }
                catch(Exception ex)
                {
                    String exception = ex.Message;
                    Exception tmpEx = ex.InnerException;
                    while(tmpEx != null)
                    {
                        exception = String.Format("{0}{1}{2}", exception, Environment.NewLine, tmpEx.Message);
                        tmpEx = tmpEx.InnerException;
                    }

                    optionalError = String.Format("Exception after processing {0} records, terminating run {1}{2}",
                        recordsProcessed,
                        Environment.NewLine,
                        exception);
                }

                // Upload everything to event hub
                EventHubUtility.ProcessMessages(threadData.Context.HubConfiguration, eventMessages);

                // Notify that job is done
                threadData.ThreadExiting?.Invoke(threadData.JobId, recordsProcessed, optionalError);
            }
        }

        private static void RecordEventHubRecords(String client, List<ThroughputMessage> messages, RequestResult result)
        {
            String hubState = String.Empty;
            if (result.State == false)
            {
                hubState = result.Response.Replace(',', '_');

                if (result.ResponseCode == 503)
                {
                    // Backoff
                    System.Threading.Thread.Sleep(100);
                }
            }
            messages.Add(new ThroughputMessage()
            {
                ClientId = client,
                Processed = DateTime.Now,
                State = result.State ? 1 : 0,
                AILatency = (float)result.ResponseTime.TotalSeconds,
                SendError = hubState
            });

        }
    }
}
