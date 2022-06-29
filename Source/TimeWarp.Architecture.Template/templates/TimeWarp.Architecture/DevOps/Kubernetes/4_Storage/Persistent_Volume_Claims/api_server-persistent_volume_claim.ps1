# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

$global:ApplicationName = "yarp"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest -file ./api_server-persistent_volume_claim.yaml -cluster $ClusterName -namespace $ApplicationNameSpace 
}
finally {
  Pop-Location
}

