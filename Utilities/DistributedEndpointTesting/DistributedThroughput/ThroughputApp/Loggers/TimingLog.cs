using System;
using ThroughputApp.Configuration;

namespace ThroughputApp.Loggers
{
    class TimingLog
    {
        private static object FileLock = new object();

        public static void RecordTiming(ThroughputConfiguration context, string content)
        {
            lock (FileLock)
            {
                if (!String.IsNullOrEmpty(context.LogDirectory))
                {
                    if (!System.IO.Directory.Exists(context.LogDirectory))
                    {
                        System.IO.Directory.CreateDirectory(context.LogDirectory);
                    }
                    String header = "Time,Record,PayloadSize,Success,Attempts,Response,TotalTime,AiTime";
                    String path = System.IO.Path.Combine(context.LogDirectory, "timing.csv");

                    bool exists = System.IO.File.Exists(path);
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path, true))
                    {
                        if (!exists)
                        {
                            writer.WriteLine(header);
                        }

                        content = String.Format("{0},{1}", DateTime.Now.ToString("HH:mm:ss:fff"), content);
                        writer.WriteLine(content);
                    }
                }
            }
        }
    }
}
