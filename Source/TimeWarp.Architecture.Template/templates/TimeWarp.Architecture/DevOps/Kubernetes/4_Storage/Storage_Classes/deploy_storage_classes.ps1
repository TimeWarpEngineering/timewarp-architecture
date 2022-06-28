Push-Location $PSScriptRoot
try { 
  
  Apply-Manifest -file managed_premium_retain-storage_class.yaml -cluster $ClusterName -namespace $ApplicationNameSpace 
}
finally {
  Pop-Location
}

