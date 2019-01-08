# Azure Subscription Cleanup Scripts - Global Stats
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

Sometimes it's helpful to get an overview of all the subscription resources across all your resources. The scripts in this directory, along with others in this repo, can be used to find out about resource groups and virtual machine usage.

Copy the three files in this repo to this directory (after cloning of course):

\ResourceGroupLevel\DelResourceGroups.ps1

\VirtualMachines\GetVmInfoAndConfig.ps1

\ResourceLevel\ListResources.ps1

Open PowerShell and navigate to the local directory of this file and run the command 
```
Login-AzureRMAccount
```

> NOTE: No resources will be affected running these scripts as prepared. Simply, it collects information across Azure subscriptions.

## Scripts

|File|Description|
|--------------------|------------------------|              
| GetSubscription.ps1|	This script collects all of the subcription information associated with the login account. It then uses the two imported scripts above to collect Virtual Machine and Resource Group information for each of the associted subscriptions. This script outputs two files: resource_group_status.json and virtual_machine_status.json|
|CollectSubStats.ps1|This script is called from GetSubscription.ps1 and rolls up the information collected outputting a file named subscription_global_stats.json in the executing directory.|


### Output Files
|Parameter |Content|
|--------------------|-----------------------|
|resource_group_status.json| For each subscription there is an object that contains subscription name, id, count of resource groups and count of unlocked resource groups.| 
|virtual_machine_status.json| For each subscription there is an object that contains subscription name, id, count of virtural machines, count of running virtual machines and count of stopped virtual machines (i.e. not deallocated).|
|subscription_global_stats.json| Lists the count of subscriptions, count of resource groups, count of unlocked resource groups, count ofvirtual machines, count of running virtual machines, and finally a list of subscription data where there is at least one resource group.|
|global_resource_usage.json| Lists a count of all Azure resource types and the number of instances of each resource type that was found accross all subscriptions.|

