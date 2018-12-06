## CARRY OVER FROM MAIN DRIVER
$resourceGroupName="dangauto"
$locationString = "eastus"


#######################################################################
# Service Bus
#######################################################################
$serviceBusDeploymentName = "servicebuscreate22"
$serviceBusNamespace = "cosmossb"
$translationQueue = "translationqueue"
$ocrQueue = "ocrqueue"
$faceQueue = "faceapiqueue"
$inspectionQueue = "inspectionqueue"

#Set up parameters to create CosmosDB account.
$serviceBusCreateParameters = @{}
$serviceBusCreateParameters.Add("serviceBusNamespaceName", $serviceBusNamespace)
$serviceBusCreateParameters.Add("serviceBusTranslationQueueName",$translationQueue)
$serviceBusCreateParameters.Add("serviceBusOCRQueueName",$ocrQueue)
$serviceBusCreateParameters.Add("serviceBusFaceQueueName",$faceQueue)
$serviceBusCreateParameters.Add("serviceBusInspectionQueueName",$inspectionQueue)
$serviceBusCreateParameters.Add("location",$locationString)


#######################################################################
# Create Service Bus and Queues
#######################################################################
Write-Host("Creating Service Bus and Queues")
New-AzureRmResourceGroupDeployment -Name $serviceBusDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\ServiceBus.json" -TemplateParameterObject $serviceBusCreateParameters

$serviceBusInfo = @{}
$serviceBusInfo.Add("connectionstring", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $serviceBusDeploymentName).Outputs.namespaceConnectionString.value)
$serviceBusInfo.Add("primarykey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $serviceBusDeploymentName).Outputs.sharedAccessPolicyPrimaryKey.value)
$serviceBusInfo.Add("name", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $serviceBusDeploymentName).Outputs.namespaceName.value)
$serviceBusInfo.Add("translationQueue",$translationQueue)
$serviceBusInfo.Add("ocrQueue",$ocrQueue)
$serviceBusInfo.Add("faceQueue",$faceQueue)
$serviceBusInfo.Add("inspectionQueue",$inspectionQueue)


Write-Host ( $serviceBusInfo["connectionstring"])
Write-Host ( $serviceBusInfo["primarykey"])
Write-Host ( $serviceBusInfo["name"])
Write-Host ( $serviceBusInfo["translationQueue"])
Write-Host ( $serviceBusInfo["ocrQueue"])
Write-Host ( $serviceBusInfo["faceQueue"])
Write-Host ( $serviceBusInfo["inspectionQueue"])


