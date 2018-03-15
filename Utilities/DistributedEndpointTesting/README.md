# Throughput Application
<sup>Written by Dan Grecoe a Microsoft Employee</sup>


With a move to managing your own k8s clusters for running an ML/AI model endpoint in Azure, it's difficult to know how much traffic your cluster can actually manage. In the end, the processing power required for your model, VM choice, and replica count make it very difficult to come up with a good formula to choose from. There are formulas, and you can find that information [here](https://github.com/grecoe/CloudAI/tree/master/Utilities/ACSScaling), but each model has different resource requirements. 

If you are simply opening an endpoint for very low volume traffic, the default cluster provided will liley be sufficient. However, if you require more horespower to handle higher volumes you will spend some time trying to figure out what is the right solution.

    - How many replicas do I need?
    - How many nodes should I provision?
    - What CPU allocation is enough for a replica?

At some point you will want to test how much traffic your cluster can handle. That is the purpose of this application, to test the throughput that your cluster can handle on a given ML/AI model endpoint on your k8s cluster. 

A single instance of an application can help you determine how much your endpoint can handle, and this application can fill that need. However, some endpoints will need more than one client excercising the endpoint to truly understand the full potential of the endpoint. This application is designed to fill that need as well. 

# What is in this project

|Directory|Content|
|-------------|-----------------------|
|DistributedThroughput|The application code that will test the endpoint.|
|SupportFiles|Queries for Azure Stream Analytics and SQL|

# What you need?

## Service Endpoint Information
The information you need to test your endpoint with this applicaiton are, minimally :

    - The endpoint URL of your ML/AI Operationalized endpoint.
    - The applicaiton key of your operationalized endpoint.
    - An understanding of the input payload to the operationalized endpoint.

All of this information is available to you through the Azure ML CLI, detailed informaiton on how gather this information and call your endpoint can be found [here](https://docs.microsoft.com/en-us/azure/machine-learning/preview/model-management-consumption)

## [Optional] Distributed Messaging Infrastructure
If multiple instances of the application are running on different machines, optional infrastructure in Azure can be configured to capture all of the statistics from each machine.

To do this, you configure the optional Azure Event Hub settings in the configuration file and set up the rest of the environment for that capture. That includes:

- An Azure Event Hub
- An Azure Stream Analytics Job
- An Azure SQL Database

As an example, there are Azure VM's set up in 3 different regions all exercising the same endpoint. The followign diagram shows what the architecture would look like. 

DAN FIX THIS WITH IMAGE

# Project Components

|Component|Purpose|
|---------|----------|
|ThroughputInterfaces|There is really only a single interface exposed in this library along with shared configuration objects. This library is used by both the main application and any provided record providers (explained later)|
|ThroughputApp|This is the main application that orchestrates the execution against the endpoint|
|RecordProviderExample|This is an example of creating your own record provider. A record provider supplies the information about the endpoint and provides a list of records that should be used to test the endpoint.|

# Overview
The ThroughputApp application will exercise a REST Endpoint that was published using Azure Machine Learning Workbench and the Azure CLI. An example on how to set one up is [here](https://github.com/grecoe/CloudAI/tree/master/Demos/AmlWorkbenchFullExample).

There are two ways to effectively exercise your endpoint:

1. Create custom code that implements an IRecordProvider (below) that provides endpoint information, gathers data from an Azure Storage account, and formats it in a way the endpoint expects.
2. Use a build in IRecordProvider that reads a file from your local disk that has the payload already formatted.


# IRecordProvider Interface
The IRecordProvider interface is what you will need to implement for your given service. To do so, create a .NET Framework C# Class Library and expose a public class that implements the interface. As noted an example implementation is contained in the project RecordProviderExample:

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

# ThrougputConfiguration.json Settings
There is quite a bit of configuration that is possible, depending on what you are trying to accomplish. 

Much of it is optional, but an example configuration file with all options:

  {

    "execution": {
      "autoScaling": true,
      "autoScaleIncrement": 2,
      "threadCount": 10,
      "maxThreadCount": 100,
      "testCountPerThreadStep": 500,
      "threadStep": 2,
      "retryCount": 3,
      "clientName": "Local_App"
    },

    "logDirectory": "[LOCAL_DIRECTORY_FOR_LOGS]",
    "recordProviderDiskLocation": "[LOCAL_PATH_TO_PROVIDER_DLL]" ,
    "recordProvider": {
      "executionType": "storage",
      "recordCount": 1000,
      "storage": {
        "storageAccount": "[STORAGE_ACCOUNT_NAME]",
        "storageKey": "[STORAGE_ACCOUNT_KEY]",
        "storageContainer": "[STORAGE_ACCOUNT_CONTAINER]",
        "blobPrefix": "[STORAGE_BLOB_PREFIX]",
        "fileType": "[FILE_PREFIX i.e. .jpg]"
      },
      "local": {
        "localFile": "[LOCAL_FILE]"
      }
    },

    "defaultProvider": {
      "endpointUrl": "[SERVICE_ENDPOINT_URL]",
      "endpointKey": "[SERVICE_ENDPOINT_KEY]",
      "fileInput": "[LOCAL_FILE_WITH_JSON_PAYLOAD],
      "recordCount": 1000
    }
  }


|Configuration Item|Type|Required|Purpose|
|-----|-----|----|-----|
|execution|Object|Yes|Concurrency Settings|
|execution.autoScaling|bool|Yes|Determines if the test should find a safe level of execution. That is, thread counts start with execution.ThreadCount. If a run is succesful, the thread count is increased by execution.autoScaleIncrement. Once an error is hit, the thread count is decreased by 1 for the next run. Thread counts in auto scaling never continue to rise again after a failure. If this is set to false inital thread count is execution.ThreadCount and incremented by execution.threadStep after each run until execution.maxThreadCount is reached.|
|execution.autoScaleIncrement|int|Yes|Defines how many threads should be added to the concurrency pool if the last run is succesful and execution.autoScaling is true|
|execution.threadCount|int|Yes|Defines how many threads should be used initially|
|execution.maxThreadCount|int|Yes|Defines the maximum number of threads that should be used.|
|execution.threadStep|int|Yes|Defines the number of threads to increase between threads. For example if threadCount is 10, maxThreadCount is 20, and threadStep is 2 the runs that will be completed with use thread counts of 10,12,14,16,18, and 20.|
|execution.testCountPerThread|int|Yes|Defines how many times teh records should be sent with the current threadCount. For example, if threadCount is 10 and testCountPerThread is 4, during the test execution the 10 thread configuration will be run 4 times.|
|execution.retryCount|int|Yes|Defines how many times a single record should be retried in case of failure. The default is 1. However, there may be issues with volumne of data to the service so increasing this allows the application to retry calls to the service.|
|execution.ClientName|String|Yes|Inserted into each request as the User-Agent|
|logDirectory|String|No|The full disk path of the directory to submit log files to upon execution of a test. If this field is missing or empty there will be no log files created.|
|recordProviderDiskLocation|String|No|The full disk path of the directory containing a dll that hosts a developer created IRecordProvider object. If this field is missing or blank no additional providers will be loaded|
|recordProvider|object|No|Contains information relevant to the IRecordProvider to perform it's collection of records|
|recordProvider.executionType|string|Yes*|The expected values here are either "storage" or "local". In the case of "storage" the storage information in recordProvider.Storage should be present. In the case of local, use recordProvider.local value. However, since it is the responsiblity of the IRecordProvider to create the records, the caller is left to determine how best to use these fields.|
|recordProvider.recordCount|int|Yes*|This is the maximum number of records that the IRecordProvider should produce when requested to do so.|
|recordProvider.storage|object|Yes*|This object contains the information needed to load records from an Azure Blob Storage Account|
|recordProvider.storage.storageAccount|string|Yes*|The Azure Blob Storage Account name to read records from|
|recordProvider.storage.storageKey|string|Yes*|The Azure Blob Storage Account access key|
|recordProvider.storage.storageContainer|string|Yes*|The container within the Azure Blob Storage Account to read records from.|
|recordProvider.storage.blobPrefix|string|Yes*|The prefix to the Azure Blob API calls to filter down files from a more refined search. If this value is null, all records contained in the storage container should be considered|
|recordProvider.storage.fileType|string|Yes*|An optional string that identifies the file extension to limit the search by.|
|recordProvider.local|object|Yes**|This object contains the information needed to load records from a local file|
|recordProvider.local.localFile|string|Yes**|Full disk path to a local file|
|defaultProvider|object|No***|Object contains information about the endpoint to hit|
|defaultProvider.endpointUrl|string|Yes***|The service endpoint URL|
|defaultProvider.endpointKey|string|Yes***|The service endpoint API key|
|defaultProvider.recordCount|int|Yes***|This is the maximum number of records that the IRecordProvider should produce when requested to do so.|
|defaultProvider.fileInput|string|Yes***|Full path to a local file to use as the payload to the endpoint call.|

*recordProvider.storage is required when recordProvider.executionType is "storage"

**recordProvider.local is required when recordProvider.executionType is "local"

***defaultProvider is present

# Logging
If the configuration property loca.logDirectory is populated, three logs will be generated during the execution. All logs are created as CSV for ease of viewing content and creating meaningful graphs of execution patterns.

|File Name|Purpose|
|----------|----------|
|History.csv|Summarizes each test run|
|Timing.csv|Records every record sent to the service.|
|Scaling.csv|Shows times and thread counts when scaling up or down|


