# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

$global:ApplicationName = "web-server"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest ./web_server-service.yaml
}
finally {
  Pop-Location
}

