################################################################
#	Test compliance of resource groups and report which groups
#	are not in compliance.
################################################################

. './Utiltiies/Compliance.ps1'

# Ensure you have issued a Login-AzureRMAccount command first

# Provide your subscription to verify
$subId = 'YOUR_SUBSCRIPTION_ID' 

$complianceData = GetComplianceInformation -subId $subId

# Output the non-compliant groups.
Write-Host("#############################################")
Write-Host("Non-Compliant Groups in " + $subId)
Write-Host("#############################################")
foreach($nonCompliantGroup in $complianceData.NonCompliant.GetEnumerator())
{
	$issues = $nonCompliantGroup.Value -join ','
	Write-Host($nonCompliantGroup.Name + " " + $issues)
}

