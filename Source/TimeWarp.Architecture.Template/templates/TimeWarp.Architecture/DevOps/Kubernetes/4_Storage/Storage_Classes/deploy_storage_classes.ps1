# Validate variables
if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

Push-Location $PSScriptRoot
try { 
  Apply-Manifest managed_premium_retain-storage_class.yaml
}
finally {
  Pop-Location
}

