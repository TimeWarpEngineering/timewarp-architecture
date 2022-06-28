Push-Location $PSScriptRoot
try {   
  . "$PSScriptRoot\..\..\..\..\variables.ps1"
  Deploy-Server `
    -file ./api_server-deployment.yaml `
    -name "api-server" `
    -imageTag $ApiServerImageTag `
    -cluster $ClusterName `
    -namespace $ApplicationNameSpace `
    -environment $AspNetCore_Environment `
    -registryHost $RegistryHost 
}
finally {
  Pop-Location
}
