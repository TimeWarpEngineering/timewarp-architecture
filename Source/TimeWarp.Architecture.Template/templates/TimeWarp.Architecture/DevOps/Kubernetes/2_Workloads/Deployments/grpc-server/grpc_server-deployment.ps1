if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}
if (!$RegistryHost) { throw "RegistryHost is not set"}
if (!$AspNetCore_Environment) { throw "AspNetCore_Environment is not set"}

$global:ApplicationName = "grpc-server"
$ApplicationImageTag = "1.0.0"
$global:ApplicationImage = "$RegistryHost/$($ApplicationName):$ApplicationImageTag"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest ./grpc_server-deployment.yaml
}
finally {
  Pop-Location
}
