## CARRY OVER FROM MAIN DRIVER
$resourceGroupName="dangtest2"
$locationString = "eastus"


#######################################################################
# Function App Deployment
#######################################################################
$fnAppDeploymentName = "functionappcreate"
$fnAppName = "cosmosfn"
$fnStgType = "Standard_LRS"


#Set up parameters to create CosmosDB account.
$fnAppCreateParameters = @{}
$fnAppCreateParameters.Add("appName", $fnAppName)
$fnAppCreateParameters.Add("storageAccountType",$fnStgType)
$fnAppCreateParameters.Add("location",$locationString)

#later we need to get these from other outputs.


#######################################################################
# Create Function App
#######################################################################
Write-Host("Creating Function App")

$fnAppCreateParameters.Add("cosmosConnectionString","asdf")
$fnAppCreateParameters.Add("cosmosDatabase","sdf")
$fnAppCreateParameters.Add("cosmosInspectionCollection","sdf")
$fnAppCreateParameters.Add("cosmosProcessedCollection","asdf")
$fnAppCreateParameters.Add("cosmosIngestCollection","asdf")
$fnAppCreateParameters.Add("sbConnectionString","asdf")
$fnAppCreateParameters.Add("faceKey","asdf")
$fnAppCreateParameters.Add("faceURI","asdf")
$fnAppCreateParameters.Add("textKey","asdf")
$fnAppCreateParameters.Add("textURI","asdf")
$fnAppCreateParameters.Add("translationKey","asdf")
$fnAppCreateParameters.Add("translationURI","asdf")
$fnAppCreateParameters.Add("translationLang","asdf")
$fnAppCreateParameters.Add("visionKey","asdf")
$fnAppCreateParameters.Add("visionURI","asdf")

New-AzureRmResourceGroupDeployment -Name $fnAppDeploymentName -ResourceGroupName $resourceGroupName -TemplateFile ".\FunctionApp.json" -TemplateParameterObject $fnAppCreateParameters

$functionAppInfo = @{}
$functionAppInfo.Add("storageKey", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $fnAppDeploymentName).Outputs.storageKey.value)
$functionAppInfo.Add("storageName", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $fnAppDeploymentName).Outputs.storageName.value)
$functionAppInfo.Add("fnappname", (Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $fnAppDeploymentName).Outputs.functionAppName.value)


Write-Host ( $functionAppInfo["storageKey"])
Write-Host ( $functionAppInfo["storageName"])
Write-Host ( $functionAppInfo["fnappname"])


