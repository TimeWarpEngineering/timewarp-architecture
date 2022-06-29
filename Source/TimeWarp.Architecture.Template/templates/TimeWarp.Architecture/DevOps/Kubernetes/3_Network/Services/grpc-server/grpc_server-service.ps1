# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

$global:ApplicationName = "grpc-server"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest -file ./grpc_server-service.yaml -cluster $ClusterName -namespace $ApplicationNameSpace 
}
finally {
  Pop-Location
}

