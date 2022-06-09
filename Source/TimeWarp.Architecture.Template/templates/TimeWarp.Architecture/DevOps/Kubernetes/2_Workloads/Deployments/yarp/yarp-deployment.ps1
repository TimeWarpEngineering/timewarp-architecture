if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}
if (!$RegistryHost) { throw "RegistryHost is not set"}
if (!$AspNetCore_Environment) { throw "AspNetCore_Environment is not set"}

$global:ApplicationName = "yarp"
$ApplicationImageTag = "1.0.0"
$global:ApplicationImage = "$RegistryHost/$($ApplicationName):$ApplicationImageTag"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest ./yarp-deployment.yaml
}
finally {
  Pop-Location
}
