if (!$ApplicationNamespace) { throw "ApplicationNamespace is not set"}
if (!$ClusterName) { throw "ClusterName is not set"}

Push-Location $PSScriptRoot

try {   
  Apply-Manifest -file .\namespace.yaml -cluster $ClusterName -namespace $ApplicationNamespace 
}
finally {
  Pop-Location
}
