# Validate variables
if (!$ApplicationNamespace) { throw "ApplicationNamespace is not set"}

$global:ApplicationName = "grpc-server"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest -file ./grpc_server-service.yaml -cluster $ClusterName -namespace $ApplicationNamespace 
}
finally {
  Pop-Location
}

