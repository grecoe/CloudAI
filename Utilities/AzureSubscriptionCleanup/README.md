# Azure Subscription Cleanup Scripts
<sup>Created by Dan Grecoe, a Microsoft employee</sup>

Azure resources have a way of multiplying in a subscription that is not strictly controlled by its administrators. It happens to all of us.

On the other hand, locking down a non-production subscription that blocks your team from creating the resources they need to get their work or research done slows down the development and learning cycles. 

Managing resources, and the costs associated with them, can be a challenge. 

The scripts in this repository are meant to help you, the subscription administrator, to manage resources by:

1.	[Detecting unlocked resources groups, and deleting them.](./ResourceGroupLevel)
2.	[Identifying all resources by region and selectively deleting those resources.](./ResourceLevel)
3.	[Shutting down Virtual Machines](./VirtualMachines)

Actively managing resources can keep you in budget while still offering a wide arrange of services to your development team.

I hope the scripts in this repository will be useful to other Azure Subscription Administrators.

#### Pre-requisites
These scripts are Windows Powershell scripts that require certain functionality. The reader will need:

- A Windows based computer (local or cloud).
- An Azure Subscription 
- An up-to-date version of PowerShell: https://docs.microsoft.com/en-us/powershell/azure/install-azurerm-ps?view=azurermps-6.13.0
- This repository, cloned to the <b>hard disk</b> of the Windows based computer.
