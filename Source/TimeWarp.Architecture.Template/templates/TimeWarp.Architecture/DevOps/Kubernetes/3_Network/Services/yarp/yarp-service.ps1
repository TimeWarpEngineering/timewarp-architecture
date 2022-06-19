# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

$global:ApplicationName = "yarp"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest ./yarp-service.yaml
}
finally {
  Pop-Location
}

