
#############################################################################
#	Represents the information kept about a subscription.
#############################################################################
class Subscription {
    [string]$Id
    [string]$Name
    [string]$State
    [string]$Tenent
}

#############################################################################
#	Subscription manager determines the access to a group of subscriptions. 
#		[void] SetSubscription([Subscription]$sub)
#		[System.Collections.ArrayList] FindSubscription([string]$namePattern)
#		[Subscription] FindSubscriptionById([string]$subId)
#############################################################################
class SubscriptionManager {
	$Subscriptions = $null
	
	SubscriptionManager(){
		$this.Subscriptions = New-Object System.Collections.ArrayList
		$this.CollectSubscriptions()
	}
	
	
	#########################################################################
	#	Set a subscription so that follow on calls will work. 
	#########################################################################
	[void] SetSubscription([Subscription]$sub) {
		
		if($sub)
		{
			Write-Host("Setting account context: " + $sub.Name)
			$context = Set-AzureRmContext -SubscriptionID $sub.Id
			$context = az account set -s $sub.Id
		}
	}

	#########################################################################
	#	Find a subscription from the stored list using a name pattern. That
	#	is, whatever is passed in is configured with *NAME* in a like search.
	#	Non-unique names will return multiple entries. Return is an arraylist
	#	of Subscription instances.
	#########################################################################
	[System.Collections.ArrayList] FindSubscription([string]$namePattern) {
	
		$pattern = '*' + $namePattern + '*'
		$foundSub = $this.Subscriptions | Where-Object {$_.Name -like $pattern}
		
		$returnValue = New-Object System.Collections.ArrayList
		if($foundSub)
		{
			$foundSub | ForEach-Object {$returnValue.Add($_) > $null}
		}
		
		return $returnValue
	}
	
	#########################################################################
	#	Find a subscription from the stored list using its id. Unlike
	#	FindSubscription this call can return only a single value. 
	#########################################################################
	[Subscription] FindSubscriptionById([string]$subId){
	
		$foundSub = $this.Subscriptions | Where-Object {$_.Id -eq $subId}
		
		$returnValue = $null
		if($foundSub -and ($foundSub.Count -eq 1))
		{
			$returnValue = $foundSub[0]
		}
		
		return $returnValue
	}
	
	## PSEUDO PRIVATE 
	
	
	#########################################################################
	#	Collect all subscriptions into the internal $Subscriptions param.
	#	This does NOT clear that list first, and is called from the constructor.
	#########################################################################
	hidden [void] CollectSubscriptions() {
		$subs = Get-AzureRMSubscription

		foreach($sub in $subs)
		{
			$subObj = [Subscription]::new()
			$subObj.Id = $sub.Id
			$subObj.Name = $sub.Name
			$subObj.State = $sub.State
			$subObj.Tenent = $sub.TenentId
			
			$this.Subscriptions.Add($subObj) > $null
		}
	}
	

}



