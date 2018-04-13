# Load Testing Kubernetes
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

Today many platforms are moving towards hosting [artificial intelligence](https://azure.microsoft.com/en-us/services/machine-learning-services/) models in self-managed container services such as [Kubernetes](https://azure.microsoft.com/en-us/services/container-service/kubernetes/). At Microsoft this is a substantial change from [Azure Machine Learning Studio](https://studio.azureml.net/) which provided all the model management and operationalization services for the user automatically. 

To meet this need of self-managed container services Microsoft has introduced the [Azure Machine Learning Workbench](https://docs.microsoft.com/en-us/azure/machine-learning/preview/quickstart-installation) tool and the Azure services [Machine Learning Experimentation](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/Microsoft.MachineLearningExperimentation?tab=Overview), [Machine Learning Model Management](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/Microsoft.MachineLearningModelManagement?tab=Overview), [Azure Container Registry](https://azure.microsoft.com/en-us/services/container-registry/) and [Azure Container Service](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/microsoft.acs), 

With these new tools and services data science teams now have the freedom of wider language and model selection when creating AI new services, coupled with the choice of the infrastructure it is operationalized on. This choice enables the team to appropriately size the container service to meet the business requirements set forth for the model being operationalized while controlling costs associated with the service. 

My recent blog on [Scaling Azure Container Service Clusters](https://blogs.technet.microsoft.com/machinelearning/2018/03/20/scaling-azure-container-service-cluster/) discussed determining the required size of a Kubernetes cluster based on formulae. The formulae took into account service latency, requests per second, and the hardware it is being operationalized on. The blog notes that the formulae are a starting point for scaling the cluster. 

To move past the starting point to a production ready service requires some work in fine-tuning the model and the cluster. Fine-tuning occurs in several steps. The first step is determining the optimum CPU and Memory settings for each container in the service. The second step is working with the cluster to determine throughput and latency under a load. This blog discusses both the fine-tuning of the model and the cluster to meet project requirements.

## Discovering and Scaling Your Service on Kubernetes

Once a model has been operationalized through the [Azure CLI(Command Line Interface)](https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest) on Kubernetes you can discover and scale that service also using the Azure CLI. 

### Discover Your Service
You can discover information about your service in two ways. First is to use the [Azure Portal](http://portal.azure.com) or by using the Azure CLI. This discovery portion will provide you with the URL, the expected input, and the service API key. 

Using the Azure CLI, the following commands will provide the information.

```
> az ml service list realtime -o table
> az ml service usage realtime -i [serviceid]
> az ml service keys realtime -i [serviceid]
```


With the URL, API Key and Schema the endpoint can be exercised. Note that if the model creator did not provide a schema when operationalizing the model, that information will have to be obtained from the developer. Detailed instructions for consuming an Azure Machine Learning Web service can be found here.

Requests to an operationalized model are obtained through an HTTP POST. The request both sends and receives application/json content. Finally, you will need to set the Authorization header of the request as follows:

> Authorization: Bearer <apikey>

Before continuing, it is highly recommended that you call the endpoint at least once with the information provided to ensure that the endpoint is working correctly. If errors are received, the following command from the Azure CLI will likely show the root cause of the problem:

```
> az ml service logs realtime -i [serviceid]
```


### Scaling the Service
To scale the service with the Azure CLI it is important to ensure that ***auto-scaling*** on the service is disabled. This can be accomplished with the following command:

```
> az ml service update realtime -I [service_id] –autoscale-enabled false
```

To add more agent nodes to the cluster, or scale out the cluster, use the following command:

```
> az acs scale -n [cluster_name] -g [resource_group] --new-agent-count [agent_count]
```

### Scaling and Tuning Service Pods
To add more pods to your service, or scale up the service, use the following command: 

```
> az ml service update realtime -i [az_ml_service_id] -z [pod_count]
```

During load testing you will want to update the deployment of the service which affects all pods running the service. These updates can change the CPU, memory allocation, and pod concurrency which will be needed to while fine-tuning the service. More information on the update command can be found by using the command ***az ml service update realtime -h*** in the Azure CLI.

#### CPU Allocation
For CPU intensive models increasing the CPU allocation will increase performance. CPU allocation is a hard limit and Kubernetes will only allow you to allocate free CPU to your service pods.

```
> az ml service update realtime -i [az_ml_service_id] --cpu [i.e. 0.3, 1] 
```

#### Memory Allocation
For memory intensive models, increasing the memory will increase performance. Memory allocation is a soft limit that is not enforced by Kubernetes.

```
> az ml service update realtime -i [az_ml_service_id] --memory [i.e. 1G,500M]
```

#### Concurrency Allocation
The concurrency level on a service pod indicates queue depth to the pod. Under load this queue will be utilized to ensure that requests are not dropped. 

```
> az ml service update realtime -i [az_ml_service_id] --replica-max-concurrent-requests [i.e. 10,20]
```


> NOTE: When scaling the cluster or service Kubernetes may report errors back to the Azure CLI. The best way to ensure that the error is permanent and not transient is to use the Kubernetes dashboard to inspect whether the request was accepted or not. The next section discusses how to Configure Kubernetes Dashboard.

## Configure Kubernetes Dashboard
Determining how your model is performing on the Kubernetes cluster is a vital part of the operationalization chain. The Kubernetes dashboard allows you to see details about your cluster and the performance of individual pods running on that cluster. This section explains how to configure the Kubernetes tools to view the dashboard of any cluster created using the Azure CLI.

### Configure the Azure CLI 
The first step is to set the correct cluster environment in the Azure CLI. The following commands show the current environment, list all environments, and finally how to set a different environment. Using these commands, find the cluster you want to view the dashboard of and set it. 

```
> az ml env show
> az ml env list
> az ml env set -g [rg] -n [cn]
```

## Prepare kubectl For Your Cluster
[kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) is a command line tool for Kubernetes and runs on your client machine. This section discusses configuring the kubectl tool for use with your cluster.

With the ml environment set, navigate the command prompt to the directory carrying the kubectl config file, which on a [Windows Data Science Virtual Machine](https://azure.microsoft.com/en-us/services/virtual-machines/data-science-virtual-machines/) is C:\users\[user]\.kube and run the following command. The command will update the kubectl config file with the information about your cluster.

```
> az ml env get-credentials -n [cn] -g [rg] -i
```
Next, you want to use kubectl to open a proxy connection to the cluster enabling viewing of the Kubernetes dashboard locally. The following commands show the current kubectl configuration, view all of the configuration, and finally set the context to the configuration that points to the cluster you want to work with. Using these commands, find the cluster you want to view the dashboard of and set it. 

```
> kubectl config current-context
> kubectl config view
> kubectl config use-context [context]
```

## Launch the Kubernetes Dashboard
Finally, launch kubectl with the argument proxy which will enable you to view the dashboard:

```
>kubectl proxy
```

Once the proxy is up, paste this URL into a browser window to view the dashboard.

> http://127.0.0.1:8001/api/v1/namespaces/kube-system/services/kubernetes-dashboard/proxy/#!/node?namespace=default

## Inspecting Pods

With the Kubernetes dashboard up you can inspect many facets of the cluster. While load testing it is useful to navigate to ***Workloads/Pods*** to inspect CPU and memory usage of all of your service pods. This information will help in understanding the actual needs of the service in terms of memory and CPU. 

It is important to understand that the digital requirements of a service is dependent on the model that is chosen. Some are CPU intensive, others are memory intensive and yet others are both CPU and memory intensive. It is also important to understand this information for the service that is being operationalized. Using this information, you may choose to change the allocations for your service pods using the command from the ***Scaling and Tuning Service Pods*** section.


## Fine Tuning 
Fine tuning of the service is a very manual process, but it is necessary for determining the performance of the service endpoint. In this step it is suggested that the model get operationalized with a low low number of pods (2-4). Using a single client machine start exercising the endpoint concurrently. For each step in the process use the Kubernetes Dashboard to view the CPU and Memory usage at a pod level using the instructions in the ***Inspecting Pods*** section.  

For each step in fine tuning, the application that is used to test the endpoint should also record the latency times for each call to determine degradation as the volume of requests increases with the number of concurrent callers. That is, endpoints that are flooded with traffic will experience degradation in response times simply due to the queuing and management on the cluster itself. For this reason, collecting timing information around each request call will help determine what an expected average latency time on the endpoint is. 

If the CPU or Memory for a pod is being pegged at any step of the tests listed below, or conversely are being underutilized, use the commands in the  ***Scaling and Tuning Service Pods*** section to modify the service accordingly. 

Suggested steps for testing the service:

1.	Using a single thread, queue a group of messages and call the endpoint in rapid succession ensuring that the test period will last several minutes so that inspection of the cluster can be performed.
2.	Use several threads to perform a similar test to the previous test. At this step you should start to see degradation in the latency times. 

Once the service has been tuned to give the best performance with the minimal CPU and Memory allocations, the service is ready for load testing.

> NOTE: A simple, but useful load testing tool can be found [here](https://github.com/grecoe/CloudAI/tree/master/Utilities/DistributedEndpointTesting) in GitHub. You will need Visual Studio to build the project.

## Load Testing
Once the service has been fine-tuned, load testing of the service can begin. Using the average latency of the tuned service with several threads will give you the needed information to utilize the Pod Calculation formula from the previous post on scaling a Kubernetes cluster. Set the cluster configuration (agents/pods) according to the formula to start.

The main purpose of load testing the cluster is to determine what the throughput and latencies of the service are when the cluster is being saturated with requests. Based on the results of the tests, it will become apparent that you will need to scale the cluster either up, down or both to meet your needs. 

When tested internally our team uses a static payload, typically a file, that represents a valid payload of the service endpoint. We repeat that static payload several times, typically 1000 times, either through a single thread or evenly distributed among many threads. Using an identical payload helps to remove the unpredictability that a live data stream may inject in the testing process. 

```
NOTE: When saturating the service endpoint it is expected that the service will become overwhelmed and start to fail. This is not a bad thing. When failures begin to appear in the test results the service has hit it’s saturation point and at that point indicate it’s maximum throughput. 

This boundary is a reference point and the service should not be scaled in such a way that the average throughput saturates the service to the point of failure. Our tests have shown that a cluster that runs at 70% of it’s saturation point performs considerably better than a cluster that is running at it’s saturation point. 

The saturation point can be determined when client requests begin to receive the following HTTP Error:

503 ServiceUnavailable

Further, when the service is running at it’s saturation point the Kubernetes dashboard may showing errors on the pods, such as:
1.	Readiness probe failed: HTTP probe failed with statuscode: 502
2.	Readiness probe failed: Get http://10.244.6.84:5001/: dial tcp 10.244.6.84:5001: getsockopt: connection refused

While these errors may seem imposing, it is an indication that your service is not tuned correctly for CPU, memory, concurrency or a combination therein. However, your service may actually still be responding to requests. To ensure that the endpoint truly is still working under load, use logging in your test utility or get the service logs from the Azure CLI command listed below:

> az ml service logs realtime -i [serviceid]
```

### Single Client Load Testing
Single client load testing is useful in circumstances where the service will be called with a low frequency or will be called from a single client or gateway. 
1.	Start the test application with a small number of threads, typically less than 10. 
2.	Repeat step 1 but increase the number of threads that are communicating with the service replicating concurrency. 
3.	Repeat step 2 until errors start being returned, then reduce the concurrency/thread count on each iteration until no more errors are returned.
4.	Repeat steps 1-3 repeatedly to ensure that the results are consistent.

### Multiple Client Load Testing
Multiple client load testing is useful in circumstances where the service will be called at a higher frequency and from multiple clients or gateways. The multiple client test is identical to the single client test except that the same test is run on multiple machines against the same endpoint at the same time. 

Internally, our team configures several Azure Virtual Machines to act as the clients running the test application. These virtual machines can be:
1.	Deployed in the same Azure region as the service endpoint being tested.
2.	Deployed in different, or varying, Azure regions as the service endpoint being tested. 

Determining how the cluster is working in a multi-client scenario can be challenging. One solution that we have used is to combine the tests with other Azure services:
1.	[Azure EventHub](https://azure.microsoft.com/en-us/services/event-hubs/) : Used as a target from the client test application to upload records relating to individual requests. All clients are configured with this information.
2.	[Azure Stream Analytics](https://azure.microsoft.com/en-us/services/stream-analytics/): Used to read all events from the event hub and persist them for future investigation.
3.	[Azure SQL Server](https://azure.microsoft.com/en-us/services/sql-database/): Used to persist information from the Azure Stream Analytics job enabling simple queries against the database to determine performance.

You can view a [test tool](https://github.com/grecoe/CloudAI/tree/master/Utilities/DistributedEndpointTesting) that I use internally that not only sends the requests to the endpoint but also can be configured to have the client application send messages to an Azure EventHub. Queries for both Azure Stream Analytics and SQL can be found in that repository. 

## Conclusion
Load testing a Kubernetes cluster is a manual process. Fine tuning the model and fine tuning the cluster to meet performance goals does take time but is necessary to get it right. As I mentioned before, with great freedom comes great responsibility. 

The steps laid out in this blog will help any development team who is getting started with containerized AI models. While this blog posts focuses on Azure Kubernetes clusters using the Azure CLI, the concepts of scaling on any Kubernetes cluster whether in the cloud or at the edge still hold though the steps may vary from platform to platform. 

Dan


