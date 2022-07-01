Push-Location $PSScriptRoot
try {   
  Deploy-Server `
    -file ./web_server-deployment.yaml `
    -name "web-server" `
    -imageTag $WebServerImageTag `
    -cluster $ClusterName `
    -namespace $ApplicationNamespace `
    -environment $AspNetCore_Environment `
    -registryHost $RegistryHost 
}
finally {
  Pop-Location
}
