# Azure VM Start-Stop Script
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

The script in this folder can be used to batch start/stop Azure Virtual Machines in any number of subscriptions/resource groups.

## Files

|File|Description|
|--------------------|------------------------|              
| AzureVMStateChange.ps1|	The file that contains the script.|
| VMConfiguation.json|	This JSON file describes the location of the Virtual Machines to either start or stop. An entry is made for each subscription/resourcegroup/virtual machine that is to be affected by the script.|


## Script Parameters
The script takes two Boolean parameters

|Parameter |Usage|
|--------------------|-----------------------|
|-shutdown [$true or $false]|	A flag indicating whether the machines should be started or stopped.<br>If the machine is running and shutdown is false, the machine is skipped.<br>If the machine is stopped and shutdown is true, the machine is skipped.<br>The default value is $true (stop the machines)| 
|-deallocate [$true or $false]|	A flag that is used ONLY when -shutdown $true is passed in. Determines whether to deallocate the resources or just shut down the VM (which would still incur costs).<br>The default value is $false (does not deallocate the machine when shutdown)|

If using $true for deallocated the machine will lose it's dynamic IP address when restarted, but you will stop being billd for it. 

To create static IP's for your virtual machine, read here:

https://docs.microsoft.com/en-us/azure/virtual-network/virtual-network-public-ip-address


## Usage

Start a VM: 
> .\AzureVMStateChange.ps1 -shutdown $false

Stop a VM
> .\AzureVMStateChange.ps1 -shutdown $true

> .\AzureVMStateChange.ps1 -shutdown $true -deallocate $true
