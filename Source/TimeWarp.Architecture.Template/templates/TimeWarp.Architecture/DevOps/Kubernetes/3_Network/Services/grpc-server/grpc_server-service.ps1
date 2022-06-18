# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

$global:ApplicationName = "grpc-server"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest ./grpc_server-service.yaml
}
finally {
  Pop-Location
}

