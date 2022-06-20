# load variables
. "$PSScriptRoot\settings.ps1"

# ensure required variables are set
if (!$BaseName) { throw "BaseName is not set"}

Write-Output "Deleting resource group: $BaseName-rg"
az group delete -n $BaseName-rg
az deployment sub delete --debug -n $BaseName
az deployment sub wait --debug --deleted -n $BaseName
