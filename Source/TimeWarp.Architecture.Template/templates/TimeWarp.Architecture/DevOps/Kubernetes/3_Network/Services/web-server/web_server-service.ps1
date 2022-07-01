# Validate variables
if (!$ApplicationNamespace) { throw "ApplicationNamespace is not set"}

$global:ApplicationName = "web-server"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest -file ./web_server-service.yaml -cluster $ClusterName -namespace $ApplicationNamespace 
}
finally {
  Pop-Location
}

