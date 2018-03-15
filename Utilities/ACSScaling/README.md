# Azure Container Service Scaling
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

Replica sets and pods and nodes…oh my!

Microsoft has created very powerful and customizable tools for the professional data scientist with Azure Machine Learning Workbench. The tool is used to create container images for Machine Learning or Artificial Intelligence models and exposing them as a REST API endpoint deployed and managed on an Azure Container Service. This is a very different approach than Azure Machine Learning Studio. In studio, a model is created by the data scientist who then publishes that model as a REST API endpoint with the click of a button and the entire service is then managed by Azure. 

The flexibility of Azure Machine Learning Workbench relinquishes control to the scientist and Dev Ops teams with regards to development and operationalization. That flexibility transfers responsibility of the backend service to DevOps who must that the Kubernetes cluster is scaled appropriately for the desired load and responsiveness.

This post discusses scaling a Kubernetes cluster with Azure Container Service and walks through many of the considerations of operationalizing a container created with Azure Machine Learning Workbench on the Azure Container Service but also covers basic Kubernetes considerations to meet the needs of any project.  

This blog is not meant as an introduction to Azure Machine Learning Workbench and how to develop models and publish them as a service. Those topics are well documented online.

The best place to start is by covering some very high-level basics of what a Kubernetes cluster is. 

## Kubernetes Cluster Basics
Kubernetes is an open source system used for managing, deploying and scaling containerized applications. The containers that are deployed with Azure Machine Learning Workbench  have REST API endpoints that expose ML/AI models. 

To be clear this post is also not a definitive reference on Kubernetes, however, it’s important to understand a few high-level terms about the makeup of a Kubernetes cluster. 

The cluster is a collection of virtual machines, called nodes, in which there are specific roles.

-	<b>Master</b>: Also known as the apiserver, this is where communication to the rest of the cluster occurs. A master is a virtual machine that can vary in size. 
-	<b>Agents</b>: These are the nodes in which containers, also known as a pod or replica, will reside and be executed. An agent is a virtual machine that can vary in size. 
-	<b>Pod</b>: An individual instance of a container. The configuration of a pod defines the amount of CPU usage and memory, among other settings, associated with every instance. 

For more information on Azure Virtual Machines visit the Azure documentation. Pricing for an Azure Container Service can be found at the Azure Pricing Calculator.  The calculator has pricing for the Azure Container Service (AKS) version of the container service, so when calculating the costs, include ALL machines (masters and agents) as AKS manages the master nodes on your behalf. 


<p align="center">
<b>NOTE:	To make use of the information in this document you must first disable auto-scale on your Kubernetes cluster.  </b>
</p>

## How Much Processing Power Does the Cluster Have?
Docker containers run the agent nodes in a Kubernetes cluster and request a certain amount of CPU usage and memory. While memory is dynamically allocated up to the amount requested, CPU is not and therefore must be calculated out for any cluster. For those interested in understanding more about CPU usage of an individual pod, see both the Azure documentation and the Kubernetes documentation.

To calculate the available CPU units that can be allocated out to pods, it is important to understand that both the Kubernetes and Azure ML systems have overhead that is already baked into the cluster on creation. The services that are pre-installed on the cluster are:


|Service|CPU Usage|Description|
|----------|--------------|----------|
|Logging|.2|Logging system on the agent used by the system and other pods.|
|Kubernetes|~.5|A variety of Kubernetes services that support the infrastructure.|
|Azure ML Front End|1.3|Regardless of cluster size there are 3 pods that run the Azure ML frontend services. These appear in the Kubernetes dashboard as pods with the name azureml-fe-[unique-id]. If available, each pod will be served by a unique agent node in the cluster.|

## Calculating CPU Availability / Pod limits
To determine the processing capacity of a cluster, let us use an example cluster that has 3 – D3 virtual machines as agents. The D3 virtual machine contains 4 CPU. 

Each of the agent nodes will have pods associated with logging, the Kubernetes system, and the Azure ML front end: 

<p align="center">
<img src="https://github.com/grecoe/CloudAI/blob/master/Utilities/ACSScaling/images/cpuconfig1.png?raw=true" />
</p>

As the diagram shows, these resources reduce available CPU on the cluster and hence the processing capacity for our own containers. Of the 12 original CPU, 6 of them are consumed by the core services leaving 6 available to run our containers. 

For the sake of this discussion, let’s assume each of our containers requires .5 CPU to process requests in a timely manner. With this configuration there is space for 12 pods. 

To take it one step further, lets add one more agent node to the cluster. Since the three Azure ML Front End pods have already been located on existing agents, the next node will only have .7 CPU allocated for the logging and Kubernetes service leaving 3.3 CPU free for allocating to our pods:

<p align="center">
<img src="https://github.com/grecoe/CloudAI/blob/master/Utilities/ACSScaling/images/cpuconfig2.png?raw=true" width="200" height="200" />
</p>

# Understanding Your Performance and Requirements
Performance requirements are something every project will differ on. Do responses from the service need occur in a specific time frame, i.e. the service latency must be under 500ms to produce a result? Is there a requirement on how many requests must be serviced per second? Is the model CPU bound or memory bound? Understanding service performance is just as crucial to cluster configuration as any other requirement. The model chosen for the service will also greatly affect performance of the cluster. To understand the model requirements the service should be tested under a load to determine the maximum latencies and system requirements of each service call. 

## Basic Useful Formulae
There are some basic formulae that can help jump start the journey to determining a suitable cluster size for a given service. 

## Available CPU 
Understanding the system overhead allows for a simple formula to determining available CPU on a cluster. 

<p align="center">
<img src="https://github.com/grecoe/CloudAI/blob/master/Utilities/ACSScaling/images/formula1.png?raw=true" />
</p>

<p align="center">
<img src="https://github.com/grecoe/CloudAI/blob/master/Utilities/ACSScaling/images/formula2.png?raw=true" />
</p>

<p align="center">
<img src="https://github.com/grecoe/CloudAI/blob/master/Utilities/ACSScaling/images/formula3.png?raw=true" />
</p>

<p align="center">
<img src="https://github.com/grecoe/CloudAI/blob/master/Utilities/ACSScaling/images/formula4.png?raw=true" />
</p>
