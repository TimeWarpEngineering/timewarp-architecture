if (!$ApplicationNameSpace) { throw "ApplicationNameSpace is not set"}

Push-Location $PSScriptRoot

try {   
  Apply-Manifest -file .\namespace.yaml -cluster $ClusterName -namespace $ApplicationNameSpace 
}
finally {
  Pop-Location
}
