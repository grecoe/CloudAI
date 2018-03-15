using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ThroughputApp.Configuration;
using ThroughputApp.DefaultProvider;
using ThroughputInterfaces;

namespace ThroughputApp.Utilities
{
    class ProviderLocationUtility
    {

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
