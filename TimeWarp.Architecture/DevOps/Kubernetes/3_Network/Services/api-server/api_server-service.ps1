# Validate variables
if (!$ApplicationNamespace) { throw "ApplicationNamespace is not set"}

$global:ApplicationName = "api-server"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest -file ./api_server-service.yaml -cluster $ClusterName -namespace $ApplicationNamespace 
}
finally {
  Pop-Location
}

