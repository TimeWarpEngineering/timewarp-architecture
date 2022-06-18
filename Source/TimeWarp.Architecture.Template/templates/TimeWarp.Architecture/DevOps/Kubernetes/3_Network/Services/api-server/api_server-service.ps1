# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

$global:ApplicationName = "api-server"

Push-Location $PSScriptRoot
try { 
  Apply-Manifest ./api_server-service.yaml
}
finally {
  Pop-Location
}

