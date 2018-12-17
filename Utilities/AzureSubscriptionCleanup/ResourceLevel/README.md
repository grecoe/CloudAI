# Azure Subscription Cleanup Scripts - Resource Level
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

The scripts in this folder can be used to batch clean identify and clean up resources in your Azure Subscription.

Resources have a way of multiplying in an Azure Subscription over time. Some of them become obsolete but it can be unclear who created it and for what purpose. 

Use the scripts to identify and clean up resources.

## Locked or ReadOnly Resources
A resource cannot be deleted if it or it's parent resource group have either a ReadOnly or Delete lock applied to it. Even a single resource contained in the resource group having any lock prevents the deletion of resources in the resource group. 

Before you attempt to clean up resources, ensure that locks will not prevent the clean up. 

## Identify all resources in an Azure Subscription

|File|Description|
|--------------------|------------------------|              
| ListResources.ps1|	This scirpt is used to collect a list of all resources in the subscription organized by region. The results simply provide a count of each type of resource found. The results are printed out to a file.|



### Script Parameters
|Parameter |Usage|
|--------------------|-----------------------|
|-subId "id"|	The subscripiton ID to use for finding resource groups to delete.| 
|-help|	A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script will output a text file in the script directory named [subid]_resources.json.

## Identify all resources of a given resource type

|File|Description|
|--------------------|------------------------|              
| ExpandResourceType.ps1|	The script is used to collect a list of all resources of a given type in the subscription organized by region. The results simply provide a list of names and resource id of the resources found. The results are printed to a file.|



### Script Parameters
|Parameter |Usage|
|--------------------|-----------------------|
|-subId "id"|	The subscripiton ID to use for finding resource groups to delete.| 
|-resourceType "type"| The type of resource type to find. Example: Microsoft.BatchAI/jobs|
|-help|	A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script will output a text file in the script directory named [subid]_details.json.

