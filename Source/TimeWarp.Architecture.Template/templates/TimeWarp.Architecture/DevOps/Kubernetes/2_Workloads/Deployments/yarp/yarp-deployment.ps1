Push-Location $PSScriptRoot
try {   
  Deploy-Server `
    -file ./yarp-deployment.yaml `
    -name "yarp" `
    -imageTag $YarpServerImageTag `
    -cluster $ClusterName `
    -namespace $ApplicationNamespace `
    -environment $AspNetCore_Environment `
    -registryHost $RegistryHost 
}
finally {
  Pop-Location
}
