using System;
using System.Collections.Generic;
using ThroughputInterfaces.Configuration;

namespace ThroughputInterfaces
{

    public interface IRecordProvider
    {
        /// <summary>
        /// Endpoint that is the target of this test
        /// </summary>
        String EndpointUrl { get; }
        /// <summary>
        /// Key for the endpoint target
        /// </summary>
        String EndpointKey { get; }

        /// <summary>
        /// Loads the number of records identified in RecordProviderConfiguration.RecordCount and returns them as an 
        /// enumeration of key value pairs.
        /// </summary>
        /// <param name="configuration">Confiuguraiton indicating how/where to load records from.</param>
        /// <param name="onStatus">Optional delegate to report status to calling application</param>
        /// <returns>Enumeration of key value pairs
        /// key = unique record identifier
        /// value = Request payload. 
        ///     If this is a string it is passed raw as the payload to the endpoint
        ///     If this is an object it is deserialized with JSON.net to create the payload
        ///</returns>
        IDictionary<int, object> LoadRecords(RecordProviderConfiguration configuration, OnStatusUpdate onStatus = null);
    }
}
