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

        static JobExecutionEngine Engine { get; set; }

        static JobManager Manager { get; set; }

        static void Main(string[] args)
        {
            try
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
                    if (Program.SelectedProvider.GetType().IsAssignableFrom(typeof(DefaultRecordProvider)))
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
                }
            }
            catch(Exception ex)
            {
                String msg = String.Format("Exception Caught During Execution{0}{1}",
                    Environment.NewLine,
                    ex.Message);
                Console.WriteLine(msg);
            }

            Console.ReadLine();
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
