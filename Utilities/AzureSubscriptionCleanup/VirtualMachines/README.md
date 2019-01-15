# Azure VM Start-Stop Script
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

Virtual machines can proliferate in an Azure Subscription quite easily. 

Batch machines, Data Science Virtual Machines, etc. are very useful in many types of projects. This is particularly true when working with Data Scientists. 

The cost difference between a CPU machine and a GPU machine can vary widely and costs associated with virtual machines can grow quite rapidly. 

However, in many cases virtual machines are not needed all the time and <b>should</b> be shut down when not in use. This practice is not always followed.

The scripts in this repository are used for finding virtual machines and shutting them down. They do not, however, delete virtual machines from the subscription. 

>NOTE Virtual machines have two states of shutdown. First is simply to shut it down. In the shutdown state the machine is still being billed on the hourly rate as if it were running. Second is to deallocate the virtual machine. A deallocated virtual machine accrues only storage costs and not the full cost of the virtual machine. 


## Start Stop Virtual Machines
This script is used for batch start/stop Azure Virtual Machines in any number of subscriptions/resource groups. 

As noted above, a stopped versus a deallocated virtual machine are billed at very different levels. However, a deallocated virtual machine will NOT maintain it's IP address after being deallocated. If you need the machine to maintain an IP address you must first make the IP address static by using the [Azure Portal](https://portal.azure.com).

You can make your public IP address static by following the instructions on [this](https://docs.microsoft.com/en-us/azure/virtual-network/virtual-network-deploy-static-pip-arm-portal) link. You can make your private IP address static by following the instructions on [this](https://docs.microsoft.com/en-us/azure/virtual-network/virtual-networks-static-private-ip-arm-pportal) link in the section "How to retrieve static private IP address information for a VM".

### Start Stop Virtual Machines script
While you can set up schedules for virtual machines in the Azure Portal [described here](https://docs.microsoft.com/en-us/azure/automation/automation-solution-vm-management), you can also do that through this script. 

The script expects a configuration file in JSON format to idenitfy virtual machines to act upon. 

The script allows the user to both start and stop virtual machines with an additional flag to deallocate the virtual machines.

|File|Description|
|--------------------|------------------------|              
| AzureVMStateChange.ps1|	The file that contains the script.|
| VMConfiguation.json|	This JSON file describes the location of the Virtual Machines to either start or stop. An entry is made for each subscription/resourcegroup/virtual machine that is to be affected by the script.|


### Script Parameters
The script takes two Boolean parameters

|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-shutdown [$true or $false]| Yes|	A flag indicating whether the machines should be started or stopped.<br>If the machine is running and shutdown is false, the machine is skipped.<br>If the machine is stopped and shutdown is true, the machine is skipped.<br>The default value is $true (stop the machines)| 
|-deallocate [$true or $false]|	No| A flag that is used ONLY when -shutdown $true is passed in. Determines whether to deallocate the resources or just shut down the VM (which would still incur costs).<br>The default value is $false (does not deallocate the machine when shutdown)|
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|

>NOTE Remember to configure your IP addresses to static if deallocating the machines. ss


### Usage

Start a VM: 
```
.\AzureVMStateChange.ps1 -shutdown $false
```

Stop a VM
```
.\AzureVMStateChange.ps1 -shutdown $true

.\AzureVMStateChange.ps1 -shutdown $true -deallocate $true
```

## Create Configuration.json from a Subscription 
The script AzureVMStateChange.ps1 expects the Configuration.json file to contain information about virtual machines in a subscription. Creating this by hand can take time as you need to navigate through the subscription using the Azure Portal to identify the virtual machiens.

This script is can be used to create that Configuraiton.json file required. While it will contain all Virtual Machines in the subscription, it's easier to remove machines from this list than create one by hand. 

### Files

|File|Description|
|--------------------|------------------------|              
| GetVmInfoAndConfig.ps1|	The file that contains the script.|


### Script Parameters
The script takes two Boolean parameters

|Parameter |Required|Usage|
|--------------------|---------|-----------------------|
|-subId "id"| Yes|	The subscripiton ID to use for finding all virtual machines in all resource groups.| 
|-login| No| A flag, when present means user should be logged in, otherwise assumes user is logged in.|
|-help|	No| A flag indicating to show the usage of the script. Nothing will be performed.|


### Script Output
This script creates two files in the script directory:

|File |Content|
|--------------------|-----------------------|
|config_[subid].json|	A JSON file that if renamed to VMConfiguration.json can be sent to teh AzureVMStateChange.ps1 script. The contents are only virtual machines in the subscription that are in the running state.| 
|status_[subid].json|	A JSON file containing all of the VM's in the subscription broken down by region and running state|
