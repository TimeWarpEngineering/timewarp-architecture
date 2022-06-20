if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

Push-Location $PSScriptRoot

try {   
  Apply-Manifest .\namespace.yaml
}
finally {
  Pop-Location
}
