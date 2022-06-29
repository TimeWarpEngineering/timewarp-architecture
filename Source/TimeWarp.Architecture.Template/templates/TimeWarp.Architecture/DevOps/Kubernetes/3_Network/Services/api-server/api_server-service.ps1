# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

$global:ApplicationName = "api-server"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest -file ./api_server-service.yaml -cluster $ClusterName -namespace $ApplicationNameSpace 
}
finally {
  Pop-Location
}

