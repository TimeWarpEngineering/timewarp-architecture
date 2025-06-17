# Validate variables
if (!$ApplicationNamespace) { throw "ApplicationNamespace is not set"}

$global:ApplicationName = "yarp"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest -file ./api_server-persistent_volume_claim.yaml -cluster $ClusterName -namespace $ApplicationNamespace 
}
finally {
  Pop-Location
}

