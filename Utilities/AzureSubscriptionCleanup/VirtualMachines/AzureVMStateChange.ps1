####################################################################
# Script parameters:
#	Both parameters are boolean and must be set with a $true or $false.
#	-shutdown : True shuts it down, false starts it up.
#	-deallocate : Deallocates the VM instead of just stopping it. 
#				  Deallocating removes the costs from the sub.
####################################################################

param(
	[bool]$shutdown=$true,
	[bool]$deallocate=$false
	[switch]$login=$false,
	[switch]$help=$false
)

Set-StrictMode -Version 1


#####################################################
# If the help switch was used, just show the help for the 
# script and get out.
#####################################################
if($help -eq $true)
{
	Write-Host ""
	Write-Host "This script will batch change the running state of Azure Virtual Machines based on the configuration"
	Write-Host "in the Configuration.json file."
	Write-Host ""
	Write-Host "The Configuration.json file can be filled in by hand or auto generated using the GetVmInfoAndConfig.ps1 script."
	Write-Host ""
	Write-Host "Parameters:"
	Write-Host "	-shutdown : Required on all calls EXCEPT help. Identifies whether the Virtual Machines will be started or stopped."
	Write-Host "	-deallocate : If shutdown is $true, the deallocate flag determines if the Virtual Machines will be shutdown or deallocated."
	Write-Host "	-login : Tells script to log into azure subscription, otherwise assumes logged in already"
	break
}

####################################################################
# Load the configuration file
####################################################################
$configfile = ".\\VMConfiguration.json"
$configuration = $null
try
{
	$configuration = Get-Content -ErrorAction Stop -Raw -Path $configfile | ConvertFrom-Json 
}
catch
{
	Write-Output "Configuration file $configfile could not be found"
	break
}

####################################################################
# Login to Azure and set the correct subscription
####################################################################
if($login -eq $true)
{
	Write-Host "Log into Azure...."
	Login-AzureRmAccount
}
else
{
	Write-Host "Bypassing Azure Login...."
}


####################################################################
# Loop through each subscription
####################################################################
foreach($sub in $configuration.Subscriptions)
{
	Write-Output "Setting context to subscription - $($sub.SubscriptionId)"
	Set-AzureRmContext -SubscriptionID $sub.SubscriptionId

	####################################################################
	# Loop through each resource group for the subscription
	####################################################################
	foreach($rg in $sub.ResourceGroups)
	{
		####################################################################
		# Loop through each noted virtual machine
		####################################################################
		Write-Output "Resource Group - $($rg.Name)"
		foreach ($vmname in $rg.VirtualMachines)
		{
			try
			{
				$vm = Get-AzureRmVM -ErrorAction Stop -Status -ResourceGroupName $rg.Name -Name $vmname
				if($vm)
				{
					Write-Output "Working Virtual Machine - $vmname"
					$running=$false
					$stopped=$false
					foreach($status in $vm.Statuses)
					{
						Write-Output "$($status.code)"
						if($status.code -eq "PowerState/running")
						{
							$running=$true
							break
						}
						elseif($status.code -eq "PowerState/stopped")
						{
							$stopped=$true
							break
						}
						elseif($status.code -eq "PowerState/deallocated")
						{
							$stopped=$true
							break
						}
					}

					if($shutdown)
					{
						if($running)
						{
							Write-Output "$vmname shutting down, deallocating $deallocate"
							if($deallocate)
							{
								Stop-AzureRmVM -ResourceGroupName $rg.Name -Name $vmname -Force
							}
							else
							{
								Stop-AzureRmVM -ResourceGroupName $rg.Name -Name $vmname -Force -StayProvisioned
							}
						}
						else
						{
							Write-Output "$vmname requested shutdown but is currently not in a running state."
						}
					}
					else
					{
						if($stopped)
						{
							Write-Output "$vmname requested start up"
							Start-AzureRmVM -ResourceGroupName $rg.Name -Name $vmname
						}
						else
						{
							Write-Output "$vmname requested start but is not in a stopped state."
						}
					}
				}
				else
				{
					Write-Output "Machine $vmname could not be found"
				}
			}
			catch
			{
				Write-Output "Machine $vmname could not be found"
			}
		}
	}
}
