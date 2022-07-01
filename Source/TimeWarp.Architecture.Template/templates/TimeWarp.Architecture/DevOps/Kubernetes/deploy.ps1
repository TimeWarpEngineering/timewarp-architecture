
Push-Location $PSScriptRoot
try {   
  Import-Module .\PowerShell\TimeWarp.Charts\TimeWarp.Charts.psm1
  . "$PSScriptRoot\..\variables.ps1"

  if (!$SubscriptionName) { throw "SubscriptionName is not set"}
  if (!$ApplicationNamespace) { throw "ApplicationNamespace is not set"}
  if (!$ClusterName) { throw "ClusterName is not set"}

  az account set --subscription $SubscriptionName
  kubectl config use-context $ClusterName
  .\0_Namespaces\namespace.ps1
  kubectl config set-context $ClusterName --namespace $ApplicationNamespace
  .\2_Workloads\Deployments\api-server\api_server-deployment.ps1
  .\2_Workloads\Deployments\web-server\web_server-deployment.ps1
  .\2_Workloads\Deployments\grpc-server\grpc_server-deployment.ps1
  .\2_Workloads\Deployments\yarp\yarp-deployment.ps1
  .\3_Network\Services\api-server\api_server-service.ps1
  .\3_Network\Services\web-server\web_server-service.ps1
  .\3_Network\Services\grpc-server\grpc_server-service.ps1
  .\3_Network\Services\yarp\yarp-service.ps1
  .\4_Storage\Storage_Classes\deploy_storage_classes.ps1
  .\4_Storage\Persistent_Volume_Claims\api_server-persistent_volume_claim.ps1
}
finally {
  Pop-Location
}
