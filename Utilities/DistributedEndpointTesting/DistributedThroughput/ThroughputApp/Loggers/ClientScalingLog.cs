using System;
using ThroughputApp.Configuration;

namespace ThroughputApp.Loggers
{
    class ClientScalingLog
    {
        private static object FileLock = new object();

        public static void RecordScalingChange(ThroughputConfiguration context, int scale)
        {
            lock (FileLock)
            {
                if (!String.IsNullOrEmpty(context.LogDirectory))
                {
                    if (!System.IO.Directory.Exists(context.LogDirectory))
                    {
                        System.IO.Directory.CreateDirectory(context.LogDirectory);
                    }

                    String header = "Time,Scale";
                    String path = System.IO.Path.Combine(context.LogDirectory, "scaling.csv");

                    bool exists = System.IO.File.Exists(path);
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path, true))
                    {
                        if (!exists)
                        {
                            writer.WriteLine(header);
                        }

                        writer.WriteLine(String.Format("{0},{1}", DateTime.Now.ToUniversalTime().ToString("O"), scale));
                    }
                }
            }
        }
    }
}
