using System;
using ThroughputApp.Configuration;

namespace ThroughputApp.Loggers
{
    class HistoryLog
    {
        private static object FileLock = new object();

        public static void RecordHistory(ThroughputConfiguration context, string content)
        {
            lock (FileLock)
            {
                if (!String.IsNullOrEmpty(context.LogDirectory))
                {
                    if(!System.IO.Directory.Exists(context.LogDirectory))
                    {
                        System.IO.Directory.CreateDirectory(context.LogDirectory);
                    }

                    String header = "ThreadCount,Records,Failures,AITime,TotalTime,RPS,MaxRecords,MinRecords";
                    String path = System.IO.Path.Combine(context.LogDirectory, "history.csv");

                    bool exists = System.IO.File.Exists(path);
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path, true))
                    {
                        if (!exists)
                        {
                            writer.WriteLine(header);
                        }

                        writer.WriteLine(content);
                    }
                }
            }
        }
    }
}
