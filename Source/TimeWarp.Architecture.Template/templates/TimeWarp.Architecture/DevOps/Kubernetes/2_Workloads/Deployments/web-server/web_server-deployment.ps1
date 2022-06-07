if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}
if (!$RegistryHost) { throw "RegistryHost is not set"}

$global:ApplicationName = "web-server"
$ApplicationImageTag = "1.0.0"
$global:ApplicationImage = "$RegistryHost/$($ApplicationName):$ApplicationImageTag"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest ./web_server-deployment.yaml
}
finally {
  Pop-Location
}
