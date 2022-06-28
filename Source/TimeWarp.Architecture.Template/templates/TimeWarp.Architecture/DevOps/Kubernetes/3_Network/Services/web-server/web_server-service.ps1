# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

$global:ApplicationName = "web-server"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest -file ./web_server-service.yaml -cluster $ClusterName -namespace $ApplicationNameSpace 
}
finally {
  Pop-Location
}

