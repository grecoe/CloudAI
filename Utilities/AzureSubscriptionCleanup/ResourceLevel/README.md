# Azure Subscription Cleanup Scripts - Resource Level
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

It can be very useful to an administrator to understand all of the resources that are contained within a subscription. 

This high level view gives a broad understanding of what is contained in the subscription and could be used to determine where the bigger costs are being accrued. 

The scripts in this repository can be used to clean identify resources, but not delete them.

While the scripts below do not directly delete resources, they can be modified quite easily to call [Remove-AzureRMResource](https://docs.microsoft.com/en-us/powershell/module/azurerm.resources/remove-azurermresource?view=azurermps-6.13.0), which will delete individual resources from the subscription.

## Identify all resources
Having a list of all resources in a subscription can be useful in that it gives a broad picture of consumption in a subscription. 

This script is used to generate a list of all resources, broken out by Azure Region, contained in a subscription.

### Identify all resources script

|File|Description|
|--------------------|------------------------|              
| ListResources.ps1|	This scirpt is used to collect a list of all resources in the subscription organized by region. The results simply provide a count of each type of resource found. The results are printed out to a file.|


### Script Parameters
|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The subscripiton ID to use for finding resource groups to delete.| 
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script will output a text file in the script directory named [subid]_resources.json.


## Find multiple resources of a certain type
At times, it's useful to figure out where certain resources are deployed in your subscription. This can be useful in cases where a certain resource type is being taken out of production, or you simply want to know where all instances of a particular resource are located, for instance SQL Servers.

### Find multiple resources script 
|File|Description|
|--------------------|------------------------|              
| FindResourceDeployments.ps1|	The script is used to collect a list of found particular resources in the subscription organized by resource group. The results file.|

### Script Parameters
|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The subscripiton ID to use for finding resource groups to delete.| 
|-resourceFile "path"| Yes| The path to the file containing the resource types to find.|
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|

### Input File Format
The file pointed to by the parameter -resourceFile MUST be in the format:

```
{
	"resources" : [
	    "resource_type (i.e. Microsoft.MachineLearningModelManagement/accounts etc....)",
		"resource_type"
        ...
	]
}
```

### Script Output
This script will output a text file in the script directory named subid\resourcedeployment.json.


## Identify resources by type and optionally delete them
Instead of a list of all resources, it is sometimes useful to find all resources of a certain type such as storage accounts. 

This script is used to generate a list of all resources of a specific type, broken out by Azure Region, contained in a subscription.

>NOTE If deletion is chosen the resource group and/or the resource can have no locks associated with it. This follows the same rules as trying to delete a resource group. 

### Identify resources by type script
|File|Description|
|--------------------|------------------------|              
| ExpandResourceType.ps1|	The script is used to collect a list of all resources of a given type in the subscription organized by region. The results simply provide a list of names and resource id of the resources found. The results are printed to a file.|



### Script Parameters
|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The subscripiton ID to use for finding resource groups to delete.| 
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-resourceType "type"| Yes| The type of resource type to find. Example: Microsoft.BatchAI/jobs|
|-resourceGroup "groupname"| No| If provided it, the search will only look for resources of type resourceType in the specified resourceGroup|
|-deleteResources | No| A flag indicating whether found resources should be deleted. The default value is that no resources will be deleted. User will be prompted if deletion is requested.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script will output a text file in the script directory named [subid]_details.json.

### Examples

In the following examples the search will be for storage accounts.

<b> Parameters </b>
```
    $subscription="YourSubscriptionId"
    $resourceType="Microsoft.Storage/storageAccounts"
    $resourceGroup="YourResourceGroupName"
```

<b>Find all Storage Accounts </b>
```
.\ExpandResourceType.ps1 -subid $subscription -resourceType $resourceType
```

<b>Find all Storage Accounts in a specific resource group</b>
```
.\ExpandResourceType.ps1 -subid $subscription -resourceGroup $resourceGroup -resourceType $resourceType
```

<b>Find all delete Storage Accounts in a specific resource group</b>
```
.\ExpandResourceType.ps1 -subid $subscription -resourceGroup $resourceGroup -resourceType $resourceType -deleteResources
```
