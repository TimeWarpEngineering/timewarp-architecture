# load variables
. "$PSScriptRoot\..\variables.ps1"
# ensure required variables are set
if (!$BaseName) { throw "BaseName is not set"}
if (!$Location) { throw "Location is not set"}
if (!$SubscriptionName) { throw "SubscriptionName is not set"}
if (!$ResourceGroupName) { throw "ResourceGroupName is not set"}
if (!$ClusterName) { throw "ClusterName is not set"}

az account set --subscription $SubscriptionName
if ($LASTEXITCODE -ne 0) { throw "Unable to set subscription: $SubscriptionName"}

$PrincipalId = $(az ad signed-in-user show --query 'id' -o tsv)

az deployment sub validate `
  --debug `
  --name $BaseName `
  --location $Location `
  --template-file subscription.bicep `
  --parameters `
  basename=$BaseName `
  location=$Location `
  principalId=$PrincipalId
