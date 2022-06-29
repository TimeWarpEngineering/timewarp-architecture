# load variables
. "$PSScriptRoot\settings.ps1"

# ensure required variables are set
if (!$BaseName) { throw "BaseName is not set"}

az account set --subscription $SubscriptionName

Write-Output "Deleting resource group: $BaseName-rg"
az group delete --name "$BaseName-rg" --yes
az deployment sub delete --debug --name $BaseName
az deployment sub wait --debug --deleted --name $BaseName
az keyvault purge --name "$($BaseName)-kv"
az appconfig purge --name "$($BaseName)appconfig" --yes
