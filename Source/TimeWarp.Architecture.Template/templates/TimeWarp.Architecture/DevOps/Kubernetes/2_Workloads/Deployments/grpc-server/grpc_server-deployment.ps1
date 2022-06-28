Push-Location $PSScriptRoot
try {   
  Deploy-Server `
    -file ./grpc_server-deployment.yaml `
    -name "grpc-server" `
    -imageTag $GrpcServerImageTag `
    -cluster $ClusterName `
    -namespace $ApplicationNameSpace `
    -environment $AspNetCore_Environment `
    -registryHost $RegistryHost 
}
finally {
  Pop-Location
}
