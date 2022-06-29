# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

$global:ApplicationName = "yarp"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest -file ./yarp-service.yaml -cluster $ClusterName -namespace $ApplicationNameSpace 
}
finally {
  Pop-Location
}

