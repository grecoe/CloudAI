# Azure CLI Cheat Sheet
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

For any command on the Azure CLI listed, you can get help by simply typing the command with a -h in the argument list.

## Azure Login
When using the CLI you need to first log in. The following series of commands will accomplish that goal:

```
>az login
>az account show
>az account list --output table
>az account set --subscription [subscriptionname]
```

Ensure you are in the right subscription using account show. To change subscriptions, use account set with a value found after executing account list.

## AML Workbench : Configure Remote Docker Host

Using a remote docker host for workbench is recommended. Once you have a machine set up and have collected the systems IP address [ip], admin user name [au], and admin password [ap], you can associate that host with the project. You just provide a connection name [cn] that you want to refer to this connection in the workbench. 

Upon completion of identifying a target, new files [cn].compute and [cn].runconfig will be created in the /aml_config folder.

The second step is to prepare the target with the appropriate base docker image and associated required dependencies.

When these steps are completed, .py and .ipynb files can be executed on the remote host.


```
>az ml computetarget attach remotedocker -n [cn] -a [ip] -u [au] -w [ap]
>az ml experiment prepare -t [cn] -c [cn]
```

## Create Kubernetes Cluster and associate with Workbench Experiment
A Kubernetes cluster is used as the operational public endpoint for the experiment. The following commands create a Kubernetes based Azure Container Service.

The default configuration (shown) creates one master and two nodes. 

Provide a cluster name [cn], a new or existing Azure Resource Group name [rg] and a region [loc]. The region is currently required to be one of the following:

> eastus2, westcentralus, australiaeast, westeurope, or southeastasia 

The second command can be used to check the status of the deployment and when completed, run the third command to set the cluster environment for the current experiment.

```
>az ml env setup --cluster -n [cn] -l [loc] -g [rg]
>az ml env show -g [rg] -n [cn]
>az ml env set -g [rg] -n [cn]
```

## Set the Model Management Account for the Experiment
The Azure Model Management tracks models, images, manifests and services that have been created and or deployed. Provide the model management account name [mma] and the resource group name [rg] that the model management account resides in.  

This will associate the provided model management with this experiment and allow creating new services.

```
>az ml account modelmanagement set -n [mma] -g [rg]
```

## Create a Service within Model Management
Creating a service can be done in discrete steps OR in a single command. Choosing one over the other depends on what you are trying to accomplish. 

If you are updating an existing service, you would want to use multiple commands to re-build an image then update a service. If you are creating a new service, you would likely choose the single command. 

At the successful completion of either path a service is created that is now accessible to callers.

In both cases there are some similar input parameters to the calls.


|Parameter        |	Usage/Meaning|
|---------------------|------------------------------------|
|[name]               |	The name you provide for the service.|
|[model]|	The model created for the experiment, for example model.pkl|
|[manname]|	A name associated with the manifest. The manifest is registered with the model management service that identifies what is needed for docker image creation.|
|[imgname]|	Docker Image Name|
|[score]|	The score.py file name that is used in the deployed service that contains the init() and run() functions for the service.|
|[schema]|	The schema file created for the service.| 
|[conda]|	The conda_dependencies.yml file for the experiment.|
|[run]|	Runtime of the service, valid selections python|spark-py|
|[modid]|	Model ID, returned by az ml model register|
|[manid]|	Manifest ID, returned by az ml manifest create|
|[imgid]|	Docker image ID, returned by az ml image create|

### Multiple Commands
```
> az ml model register -m [model] -n [model]
> az ml manifest create -n [manname] -f [score] -r [run] -i [modid] -s [schema] -c [conda]
> az ml image create -n [imgname] --manifest-id [manid] 
> az ml service create realtime --image-id [imgid] -n [name] 
```

### Single Command
```
> az ml service create realtime -m [model] -f [score] -n [name] -s [schema] -r [run] -c [conda]
```

### Updating a service
```
> az ml service update realtime -i [serviced] --image-id [imgid] 
```

## Get Deployed Service Information
Once a service has been created it’s important to collect information about that service for clients. In particular there are 4 key items required:

|Data | Purpose|
|---------------------|-----------------------------------------|
|Service URL|	The external URL of the service to call|
|Service API Key|	The API key for the service|
|Service Input Format|	The input JSON format |
|Service Output Format|	The output JSON format|


To find out any of this information, you need to get the service ID. This is done in the first command, by listing out the available services. From this table, retrieve the service id of the service and follow with the commands that follow to get the url, key and input format. 

From the az ml service usage call, you can retrieve the command to issue on the command line to test the service. Execute that command to find out the output format.

The final command can be used to see logs for the service to determine service state.


```
> az ml service list realtime -o table
> az ml service usage realtime -i [serviceid]
> az ml service keys realtime -i [serviceid]

> az ml service logs realtime -i [serviceid]
```

## Scaling a Deployment
Scaling out and up a service is necessary in many cases. Using the Azure CLI you can scale out (agents) on the cluster or up (number of pods) for your service.

### Scale Out Cluster
To scale the cluster you must first make sure that auto scaling is not enabled. Disable autoscaling first.

```
> az ml service update realtime -I [service_id] –autoscale-enabled false
```

Add more agent VM’s to the cluster
```
> az acs scale -n [cluster_name] -g [resource_group] --new-agent-count [agent_count]
```

### Scale Up Cluster
Add more pods to a service deployment
```
> az ml service update realtime -i [az_ml_service_id] -z [pod_count]
```

# Kubernetes kubectl.exe
Kubectl is a very useful tool for working with a kubernetes cluster. It can be used for discovery, diagnosis, scaling, and opening a proxy to view the kubernetes dashboard. 

## Prepare /.kube/config file
To use kubectl, you must configure it with your cluster information. This information can be found using the Azure ML CLI. First ensure that you have used az login.

The first command displays the environment currently configured for the Azure ML CLI. If this is incorrect for the cluster you are looking for, list out the environments. 
Capture the resource group [rg] and cluster name [cn] of the cluster of interest and then set the environment.


```
> az ml env show
> az ml env list
> az ml env set -g [rg] -n [cn]
```

Once the ml environment is set, navigate the command prompt to the directory carrying the kubectl config file, which on a Windows DSVM is C:\users\[user]\.kube and run the following command. This command will update the config file with the information about your cluster.


```
> az ml env get-credentials -n [cn] -g [rg] -i
```

## Setting the context in kubectl
With the config file updated, navigate the command prompt to the directory that has the file kubectl.ext which on the Windows DSVM is C:\users\[user]\bin

If running config current-context is not the context of your cluster, view the list of configurations and get the context name for the one that matches your cluster. Finally, set that context name as the current context.


```
> kubectl config current-context
> kubectl config view
> kubectl config use-context [context]
```


## Run the k8s Dashboard
Now that kubectl is configured with your cluster as the current context, start the proxy to allow you to view the dashboard running this command

```
>kubectl proxy
```

It will direct you to a URL, but generally that doesn’t work. Instead, past the following URL into a browser to bring up the dashboard:

```
http://127.0.0.1:8001/api/v1/namespaces/kube-system/services/kubernetes-dashboard/proxy/#!/node?namespace=default
```




