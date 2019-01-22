# Azure Subscription Cleanup Scripts - Global Stats
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

Sometimes it's helpful to get an overview of all the subscription resources across all your subscription, other times you want an overview of a single subscription. The scripts in this directory are used to find either cross subscription usage or general usage patterns in a single subscription. 


> NOTE: No resources will be affected running these scripts as prepared. Simply, it collects information across Azure subscriptions.

## Scripts - Subscription Overview

|File|Description|
|--------------------|------------------------|              
| SubscriptionOverview.ps1|	This script creates an overview of subscription consumption and content. The information is higher level than most of the other scripts in that it gives an overview of most usage. |

### Parameters
|Parameter | Required | Content|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The Azure Subscripiton Id to use for finding resource groups.| 
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|


### Output File
|Parameter |Content|
|--------------------|-----------------------|
|subscription_overview.json| For the identified subscripiton an the file contains an overview of usage and layout of the subscription resources|

The file is broken down into 3 sections and remain relatively high level. Other scripts in this repository can be used to collect more detailed information on any one of the following sections:

```
{
	"ResourceUsage": {
		"resource_type": "count"
	},
	"VirtualMachines": {
		"VirtualMachineStats": "Counts of total, running, deallocated and stopped"
	},
	"ResourceGroups": {
		"Regions": {
			"AzureRegion": "count"
		},
		"Older60Days": "Count of groups older than 60 days",
		"Total": "Total count of resource groups in sub",
		"Specials": "Count of special groups created by Azure"
	}
}
``` 


## Scripts - Cross Subscription

Copy the three files in this repo to this directory (after cloning of course):

\ResourceGroupLevel\DelResourceGroups.ps1

\VirtualMachines\GetVmInfoAndConfig.ps1

\ResourceLevel\ListResources.ps1

Open PowerShell and navigate to the local directory of this file and run the command 
```
Login-AzureRMAccount
```

|File|Description|
|--------------------|------------------------|              
| GetSubscription.ps1|	This script collects all of the subcription information associated with the login account. It then uses the two imported scripts above to collect Virtual Machine and Resource Group information for each of the associted subscriptions. This script outputs two files: resource_group_status.json and virtual_machine_status.json|
|CollectSubStats.ps1|This script is called from GetSubscription.ps1 and rolls up the information collected outputting a file named subscription_global_stats.json in the executing directory.|


### Output Files
|File |Content|
|--------------------|-----------------------|
|resource_group_status.json| For each subscription there is an object that contains subscription name, id, count of resource groups and count of unlocked resource groups.| 
|virtual_machine_status.json| For each subscription there is an object that contains subscription name, id, count of virtural machines, count of running virtual machines and count of stopped virtual machines (i.e. not deallocated).|
|subscription_global_stats.json| Lists the count of subscriptions, count of resource groups, count of unlocked resource groups, count ofvirtual machines, count of running virtual machines, and finally a list of subscription data where there is at least one resource group.|
|global_resource_usage.json| Lists a count of all Azure resource types and the number of instances of each resource type that was found accross all subscriptions.|

