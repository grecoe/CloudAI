# Azure Subscription Cleanup Scripts - Resource Group Level
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

Azure Resource Groups can contain a multitude of Azure resources. 

Typically, developers and data scientists will group all necessary resources into a resource group to logically group necessary components for a customer or research project. 

At times, resource groups and all of the associated resources become abandoned for any number of reasons.

The scripts in this repository can be used to clean up resource groups.


## Deleting Azure Resource Groups
When working on a project in a particular resource group we instruct the developers to put a lock on the entire resource group or at least one of the resources contained in the resource group. These flags can be used to determine if the resource group is active and/or important.

A resource group cannot be deleted if it or any of it's contained resources have either a ReadOnly or Delete lock applied to it. Even a single resource contained in the resource group having any lock prevents the deletion of the resource group itself. 

Once a group becomes inactive or is no longer necessary, developers are instructed to remove all locks from the resource group or it's contained resources. In theory, it should also be deleted but that isn't always the case. 

The script described below can be used in two different ways. 

First, it can be used to simply scan a subscription for unlocked resource groups. The result of this scan can be used to discuss the usage of unlocked resource groups in the subscription with the resource group owner. That is, is the resource group not required anymore? Will any pertinent work be lost if it is deleted? 

Second, it can be used to scan a subscription for unlocked resource groups and delete them as they are encountered. This is a harsh, though effective, way of cleaning up a subscription. 

The difference between the two options is a simple flag sent into the script before it is executed.

## Deleting Azure Resource Groups Script

|File|Description|
|--------------------|------------------------|              
| ScanResourceGroups.ps1|	The file that contains the script to delete resource groups from a given subscription. When launched you will be prompted to log into your Azure Subscription|


### Script Parameters
|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The Azure Subscripiton Id to use for finding resource groups.| 
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|
|-whatif|No| A flag used by the script to determine what resource groups would be deleted. Nothing is deleted at this time. To have resource groups cleaned up, leave this flag off of the command line. |

### Script Output
This script will output multiple text files into a sub directory named with the subscription id. 

| File | Content |
|---------------|---------------|
|deletegroups.txt|Contains a list of resource groups that would be deleted if the -whatif flag is not used. This file is used as input to the VerifyDeletedGroups.ps1 script|
|rg_status.json| A json object giving an overview of the resource groups present and current state|
|unlockedconfiguration.json| A json object that can be fed into the UnlockResourceGroups.ps1 script| 

## Verify the Deletion of Resource Groups Script
After the DelResourceGroups.ps1 script has been run without the -whatif flag, it is useful to determine if the targeted resource groups really were deleted. 

The DelResourceGroups.ps1 script emits a file called deletegroups.txt and is expected in the script directory when this script runs.

|File|Description|
|--------------------|------------------------|              
| VerifyDeletedGroups.ps1|	The file that contains the script to determine if resource groups targeted for deletion where succesfully deleted (not present in Azure) or not. <br><br> It is expected that the file output from the DelResourceGroups.ps1 file exists in the same directory as this script.|


### Script Parameters
|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The subscripiton ID to use for finding resource groups to delete.| 
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script will write to the console window the results of determining if the targeted resource groups are still present in the subscription. 


## Removing locks on a Resource Group
This script removes all resource group and child resource locks on the specified subscription. 

|File|Description|
|--------------------|------------------------|              
| UnlockResourceGroups.ps1|	The file that contains the script to remove locks on a resource group and all of it's child resources.| 


### Script Parameters
|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The subscripiton ID to use for finding resource groups to delete.| 
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script will write to the console window the results of determining if the targeted resource groups are still present in the subscription. 

## Verifying Compliance Of Resource Groups
This script verifies that all __locked__ resource groups have the following tags applied to it:
 * alias
 * project
 * expires

 If these exist, then a further check is done on the "expires" tag to determine if the group is still valid. The data in the expires tag is expected to be YYYY-MM-DD format.
 
 
|File|Description|
|--------------------|------------------------|              
| FindNonCompliantGroups.ps1|	The file that contains the script to verify tag compliance on locked resource groups.| 


### Script Parameters
|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The subscripiton ID to use for finding resource groups to delete.| 
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script will write a file called resource_group_compliance.json in a directory named after the subscription id in the executing directory of the script. 

The data contained in this JSON file are:
|Object |Value|
|--------------------|-----------------------|
|Total| The total number of resource groups looked at.| 
|Unlocked| The number of resource groups not locked. These are explicitly non-compliant.|
|Compliant|	The number of compliant resource groups (tags and expiration).|
|NonCompliant|	Key values that are Key = resource group name, Value = missing tag list|
|InvalidDate|	Key values that are Key = resource group name, Value = bad date input on tag|
|Expired|	Key values that are Key = resource group name, Value = original expiration date|

