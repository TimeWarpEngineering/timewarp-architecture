# load variables
. "$PSScriptRoot\..\variables.ps1"

# ensure required variables are set
if (!$BaseName) { throw "BaseName is not set"}

az account set --subscription $SubscriptionName

Write-Output "Deleting resource group: $ResourceGroupName"
az group delete --name $ResourceGroupName --yes
az deployment sub delete --debug --name $BaseName
az deployment sub wait --debug --deleted --name $BaseName
az keyvault purge --name $KeyVaultName
az appconfig purge --name $AppConfigName --yes
