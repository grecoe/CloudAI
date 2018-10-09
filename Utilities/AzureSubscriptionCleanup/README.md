# Azure Subscription Cleanup Scripts
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

The scripts in this folder can be used to batch clean up unused resource groups in your Azure Subscription.

Resource groups have a way of multiplying in an Azure Subscription over time. Some of them become obsolete but it can be unclear who created it and for what purpose. 

Use the scripts to clean up resource groups.

## Locked or ReadOnly Resource Groups
A resource group cannot be deleted if it or any of it's contained resources have either a ReadOnly or Delete lock applied to it. Even a single resource contained in the resource group having any lock prevents the deletion of the resource group itself. 

Before you run the scripts below, ensure you have placed a lock somewhere on or in a resource group you do not want deleted.

## Deleting Resource Groups Script

|File|Description|
|--------------------|------------------------|              
| DelResourceGroups.ps1|	The file that contains the script to delete resource groups from a given subscription. When launched you will be prompted to log into your Azure Subscription|



### Script Parameters
|Parameter |Usage|
|--------------------|-----------------------|
|-subId "id"|	The subscripiton ID to use for finding resource groups to delete.| 
|-help|	A flag indicating to show the usage of the script. Nothing will be performed.|
|-whatif|	A flag used by the script to determine what resource groups would be deleted. Nothing is deleted at this time. To have resource groups cleaned up, leave this flag off. |

### Script Output
This script will output a text file in the script directory containing the names of all of the resource groups that are targeted for deletion. If the -whatif flag is left out of the call this file can be used with the next script VerifyDeletedGroups.ps1 to determine if the resource group is still present or not (i.e. determine the success of the DelResourceGrups.ps1 script)

## Verify the Deletion of Resource Groups Script

|File|Description|
|--------------------|------------------------|              
| VerifyDeletedGroups.ps1|	The file that contains the script to determine if resource groups targeted for deletion where succesfully deleted (not present in Azure) or not. <br><br> It is expected that the file output from the DelResourceGroups.ps1 file exists in the same directory as this script.|



### Script Parameters
|Parameter |Usage|
|--------------------|-----------------------|
|-subId "id"|	The subscripiton ID to use for finding resource groups to delete.| 
|-help|	A flag indicating to show the usage of the script. Nothing will be performed.|

### Script Output
This script will write to the console window the results of which resource groups are present in the Azure Subscription or not. 

