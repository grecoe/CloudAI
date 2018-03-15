using System;
using System.Collections.Generic;
using ThroughputApp.Configuration;
using ThroughputInterfaces;
using ThroughputInterfaces.Configuration;

namespace ThroughputApp.DefaultProvider
{
    /// <summary>
    /// Implmentation of an IRecordProvider that uses a file on disk 
    /// for messages to send to the endpoint. The file MUST be in the raw
    /// format that is expected by the endpoint. 
    /// </summary>
    public class DefaultRecordProvider : IRecordProvider
    {
        public string EndpointUrl { get; private set; }

        public string EndpointKey { get; private set; }

        private string FileInput { get; set; }

        public IDictionary<int, object> LoadRecords(RecordProviderConfiguration configuration, OnStatusUpdate onStatus = null)
        {
            Dictionary<int, object> returnValue = new Dictionary<int, object>();

            String fileContent = System.IO.File.ReadAllText(this.FileInput);

            for(int i=0; i<configuration.RecordCount; i++)
            {
                returnValue.Add(i, fileContent);
            }

            return returnValue;
        }

        public DefaultRecordProvider(ThroughputConfiguration context)
        {
            if(!context.DefaultProvider.IsValid())
            {
                throw new Exception("Default provider is invalid");
            }

            this.EndpointUrl = context.DefaultProvider.Url;
            this.EndpointKey = context.DefaultProvider.Key;
            this.FileInput = context.DefaultProvider.File;
        }
    }
}
