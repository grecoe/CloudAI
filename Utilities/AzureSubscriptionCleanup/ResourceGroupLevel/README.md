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
| DelResourceGroups.ps1|	The file that contains the script to delete resource groups from a given subscription. When launched you will be prompted to log into your Azure Subscription|


### Script Parameters
|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The Azure Subscripiton Id to use for finding resource groups.| 
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|
|-whatif|No| A flag used by the script to determine what resource groups would be deleted. Nothing is deleted at this time. To have resource groups cleaned up, leave this flag off of the command line. |

### Script Output
This script will output a text file in the script directory named deletegroups.txt, containing the names of all of the resource groups that are targeted for deletion. That is, resource groups that are unlocked.

A summary of all resource groups and the lock state is written to the console and can be saved by redirecting the output to a file.

When executing without the -whatif flag, the deletegroups.txt file can be used with the VerifyDeletedGroups.ps1 script to determine if the selected resource groups for deleting are still present after the deletion process.


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
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script will write to the console window the results of determining if the targeted resource groups are still present in the subscription. 


## Finding potentially unused resource groups
Just because a resource group doesn't have a lock on it, dosen't mean it is not active. One way to determine a potentally stale resource group is to check the activity logs of the resource group. 

If there is no log activity there exists a strong possiblity that the resource group is dormant or abandonded. 

Of course, before manually deleting inactive resource groups, the administrator should verify with the developer responsible for the creation of the group to determine it's true state.   

|File|Description|
|--------------------|------------------------|              
| FindActiveResourceGroups.ps1|	The file that contains the script to scan a subscriptions resource groups looking at the activity logs of the resource group. 


### Script Parameters
|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The subscripiton ID to use for finding resource groups to delete.| 
|-hours "n"| No| The number of hours in the past to search. Default is 2 maximum is 15 days (though not verified, the limit on the Get-AzureRmLog command is 15 days)| 
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script creates a file named [subscription_id]activitylogs.json in the execution directory of the script. In it contains the list of active and inactive resource groups for the subscription that the script was run against. 
