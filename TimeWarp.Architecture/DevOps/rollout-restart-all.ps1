Push-Location $PSScriptRoot
try {   
  . "$PSScriptRoot\variables.ps1"
  az account set --subscription $SubscriptionName
  kubectl config set-context $ClusterName --namespace $ApplicationNamespace
  kubectl rollout restart deploy api-server
  kubectl rollout restart deploy grpc-server
  kubectl rollout restart deploy web-server
  kubectl rollout restart deploy yarp

  kubectl get pods
}
finally {
  Pop-Location
}
