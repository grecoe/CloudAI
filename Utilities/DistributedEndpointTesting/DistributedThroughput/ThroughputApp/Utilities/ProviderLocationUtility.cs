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
using System.Reflection;
using ThroughputApp.Configuration;
using ThroughputApp.DefaultProvider;
using ThroughputInterfaces;

namespace ThroughputApp.Utilities
{
    class ProviderLocationUtility
    {

        /// <summary>
        /// Loads instances of IRecordProvider using the location on disk identified
        /// in the configuration.
        /// </summary>
        /// <param name="context">App context containing disk location to look for</param>
        /// <returns>Collection of IRecordProviders</returns>
        public static List<IRecordProvider> LoadRecordProviders(ThroughputConfiguration context)
        {
            Type type = typeof(IRecordProvider);
            String providerLocation = context.RecordProviderDiskLocation;

            List<IRecordProvider> returnSelections = new List<IRecordProvider>();
            if (!String.IsNullOrEmpty(providerLocation))
            {
                // Load all assemblies in the location looking for the interface IRecordProvider
                System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(providerLocation);
                System.IO.FileInfo[] files = directory.GetFiles("*.dll", System.IO.SearchOption.TopDirectoryOnly);

                List<Assembly> assemblies = new List<Assembly>();
                foreach (System.IO.FileInfo file in files)
                {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(file.FullName);
                    assemblies.Add(AppDomain.CurrentDomain.Load(assemblyName));
                }

                IEnumerable<Type> types = assemblies
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && p.IsClass);

                // Create any record providers found
                foreach (Type t in types)
                {
                    returnSelections.Add(Activator.CreateInstance(t) as IRecordProvider);
                }
            }

            // Is there a default provider? If so add that to the list as well.
            if (context.DefaultProvider.IsValid())
            {
                returnSelections.Add(new DefaultRecordProvider(context));
            }

            return returnSelections;
        }

        /// <summary>
        /// When more than one provider is available, uses command prompt for 
        /// user to choose which provider should be used for a test.
        /// </summary>
        /// <param name="providers">List of IRecordProvider instances</param>
        /// <returns>Selected IRecordProvider</returns>
        public static IRecordProvider SelectProvider(List<IRecordProvider> providers)
        {
            IRecordProvider selectedProvider = null;

            if (providers.Count != 1)
            {
                if (providers.Count == 0)
                {
                    Console.WriteLine("There are no record providers identified");
                }
                else
                {
                    Console.WriteLine("There are {0} providers, please choose one: ", providers.Count);
                    foreach (IRecordProvider provider in providers)
                    {
                        Console.WriteLine("{0} - {1}", providers.IndexOf(provider), provider.GetType().FullName);
                    }

                    Console.Write("Selection : ");
                    String selection = Console.ReadLine();

                    int sel = int.Parse(selection);

                    selectedProvider = providers[sel];
                }
            }
            else
            {
                selectedProvider = providers.First();
            }

            return selectedProvider;
        }
    }
}
