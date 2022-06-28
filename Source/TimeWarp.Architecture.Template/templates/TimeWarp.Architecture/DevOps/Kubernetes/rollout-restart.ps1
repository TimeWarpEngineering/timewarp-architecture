Push-Location $PSScriptRoot
try {   
  . "$PSScriptRoot\..\Bicep\TimeWarp-Architecture\settings.ps1"
  az account set --subscription $SubscriptionName
  kubectl config set-context $ClusterName --namespace $Namespace
  kubectl rollout restart deploy api-server
  kubectl rollout restart deploy grpc-server
  kubectl rollout restart deploy web-server
  kubectl rollout restart deploy yarp
}
finally {
  Pop-Location
}
