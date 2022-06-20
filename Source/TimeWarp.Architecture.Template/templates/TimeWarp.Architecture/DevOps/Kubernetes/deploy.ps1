Push-Location $PSScriptRoot
try {   
  .\5_Configuration\Powershell_Variables\initialize_variables.ps1 
  .\0_Namespaces\namespace.ps1
  .\2_Workloads\Deployments\api-server\api_server-deployment.ps1
  .\2_Workloads\Deployments\web-server\web_server-deployment.ps1
  .\2_Workloads\Deployments\grpc-server\grpc_server-deployment.ps1
  .\2_Workloads\Deployments\yarp\yarp-deployment.ps1
  .\3_Network\Services\api-server\api_server-service.ps1
  .\3_Network\Services\web-server\web_server-service.ps1
  .\3_Network\Services\grpc-server\grpc_server-service.ps1
  .\3_Network\Services\yarp\yarp-service.ps1
  .\4_Storage\Storage_Classes\deploy_storage_classes.ps1
}
finally {
  Pop-Location
}
