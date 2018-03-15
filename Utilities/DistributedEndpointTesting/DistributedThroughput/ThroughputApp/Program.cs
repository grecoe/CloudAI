using System;
using System.Collections.Generic;
using System.Threading;
using ThroughputInterfaces;
using ThroughputApp.Configuration;
using ThroughputApp.Job;
using ThroughputApp.Utilities;
using ThroughputApp.DefaultProvider;

namespace ThroughputApp
{
    class Program
    {
        /// <summary>
        /// All of the configuration for this run coming from ThroughputConfiguration.json
        /// </summary>
        static ThroughputConfiguration Context { get; set; }

        /// <summary>
        /// Event used to trigger when all jobs have completed
        /// </summary>
        static ManualResetEvent ResetEvent { get; set; }
        
        /// <summary>
        /// Record provider list. There can be only one, but in case there are more, or user
        /// want's to change funcitonality to choose which provider to use.
        /// </summary>
        static List<IRecordProvider> RecordProviders = new List<IRecordProvider>();

        /// <summary>
        /// IRecordProvider selected either by user or because there was only 1
        /// </summary>
        static IRecordProvider SelectedProvider { get; set;  }

        //static IJobExecutionEngine<ThroughputConfiguration> Engine { get; set; }
        //static IJobManager<ThroughputConfiguration> Manager { get; set; }
        static JobExecutionEngine Engine { get; set; }
        static JobManager Manager { get; set; }

        static void Main(string[] args)
        {
            // Event triggered when everythign has completed
            Program.ResetEvent = new ManualResetEvent(false);

            // Load the context for the run
            Program.Context = ThroughputConfiguration.LoadConfiguration();

            // Load the record and default record providers, then select one
            Program.RecordProviders = ProviderLocationUtility.LoadRecordProviders(Program.Context);
            Program.SelectedProvider = ProviderLocationUtility.SelectProvider(Program.RecordProviders);

            if (Program.SelectedProvider != null)
            {
                // Determine the number of records being sent
                if(Program.SelectedProvider.GetType().IsAssignableFrom(typeof(DefaultRecordProvider)))
                {
                    Program.Context.SelectedRecordCount = Program.Context.DefaultProvider.RecordCount;
                }
                else
                {
                    Program.Context.SelectedRecordCount = Program.Context.RecordConfiguration.RecordCount;
                }

                // Display a summary of what is about to happen
                Program.ShowSummary();

                // Run the jobs then wait until complete.
                Console.WriteLine("Starting Jobs");
                Program.StartJobs();
                while (!WaitHandle.WaitAll(new WaitHandle[] { Program.ResetEvent }, 1000)) ;
                Console.WriteLine("Test Complete");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Print out some basic information about this run
        /// </summary>
        private static void ShowSummary()
        {
            Console.WriteLine("Record Provider: {0}", Program.SelectedProvider.GetType().FullName);
            Console.WriteLine("Provider Endpoint: {0}", Program.SelectedProvider.EndpointUrl);
            Console.WriteLine("Records Per Run: {0}", Program.Context.SelectedRecordCount);
            Console.WriteLine("Thread Scaling Enabled: {0}", Program.Context.Execution.AutoScaling?"True":"False");
            Console.WriteLine("Executions Per Thread Step : {0}", Program.Context.Execution.TestCountPerThreadStep);
            Console.WriteLine("Starting Thread Count: {0}", Program.Context.Execution.ThreadCount);
            Console.WriteLine("Maximum Thread Count: {0}", Program.Context.Execution.MaxThreadCount);
            Console.WriteLine("Incremental Thread Step : {0}", Program.Context.Execution.ThreadStep);
        }

        /// <summary>
        /// Start up a new job manager and start all jobs
        /// </summary>
        private static void StartJobs()
        {
            Program.Manager = new JobManager(Program.SelectedProvider,
                Program.Context,
                null,
                Program.ReportStatus,
                Program.ManagerAllJobsCompleted);

            Program.Engine = new JobExecutionEngine();
            Program.Engine.StatusUpdate += Program.ReportStatus;
            Program.Engine.AllThreadsCompleted += AllJobsCompleted;
            Program.Engine.Context = Program.Context;
            Program.Engine.Manager = Program.Manager;

            Program.Engine.StartExecution();
        }

        private static void ReportStatus(String report)
        {
            Console.WriteLine(report);
        }

        private static void AllJobsCompleted()
        {
            Program.ResetEvent.Set();
        }

        private static void ManagerAllJobsCompleted()
        {
            Console.WriteLine("Manager says all done");
        }
    }
}
