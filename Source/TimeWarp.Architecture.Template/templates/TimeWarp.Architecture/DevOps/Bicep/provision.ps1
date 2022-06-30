# load variables
. "$PSScriptRoot\..\variables.ps1"
# ensure required variables are set
if (!$BaseName) { throw "BaseName is not set" }
if (!$Location) { throw "Location is not set" }
if (!$SubscriptionName) { throw "SubscriptionName is not set" }
if (!$ResourceGroupName) { throw "ResourceGroupName is not set" }
if (!$ClusterName) { throw "ClusterName is not set" }
if (!$AppConfigName) { throw "AppConfigName is not set" }
if (!$KeyVaultName) { throw "AppConfigName is not set" }

Push-Location $PSScriptRoot

try {   
  az account set --subscription $SubscriptionName
  if ($LASTEXITCODE -ne 0) { throw "Unable to set subscription: $SubscriptionName" }

  $PrincipalId = $(az ad signed-in-user show --query 'id' -o tsv)

  $DeploymentOutputString = az deployment sub create `
    --debug `
    --name $BaseName `
    --location $Location `
    --template-file subscription.bicep `
    --parameters `
    basename=$BaseName `
    appconfigname=$AppConfigName `
    location=$Location `
    clustername=$ClusterName `
    principalId=$PrincipalId `
    keyvaultname=$KeyVaultName `
  
  # this will set up kube config so I can use kubectl commands and VSCode plugin uses kube.config to connect to cluster
  az aks get-credentials --resource-group $ResourceGroupName --name $ClusterName --overwrite-existing

  $DeploymentOutput = $DeploymentOutputString | ConvertFrom-Json

  $global:ConnectionStrings__AppConfig = $DeploymentOutput.properties.outputs.app_config_connectionstring.value
}
finally {
  Pop-Location
}
