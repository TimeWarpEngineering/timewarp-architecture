Push-Location $PSScriptRoot

$global:ApplicationNamespace = "timewarp"

try {   
  Apply-Manifest .\namespace.yaml
}
finally {
  Pop-Location
}
