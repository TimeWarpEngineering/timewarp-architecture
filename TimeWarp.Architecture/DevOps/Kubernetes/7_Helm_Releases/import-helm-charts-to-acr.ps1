# to see latest tags on these images 
# controller
# $(Invoke-RestMethod -Uri https://k8s.gcr.io/v2/ingress-nginx/controller/tags/list).Tags
# $(Invoke-RestMethod -Uri https://k8s.gcr.io/v2/ingress-nginx/kube-webhook-certgen/tags/list).Tags
# $(Invoke-RestMethod -Uri https://k8s.gcr.io/v2/defaultbackend-amd64/tags/list).Tags

# cert manager
# https://quay.io/repository/jetstack/cert-manager-controller?tab=tags

Push-Location $PSScriptRoot
try {   
  . "..\..\variables.ps1"

  $ControllerRegistry = "k8s.gcr.io"
  
   # controller
  $ControllerImage = "ingress-nginx/controller"
  $ControllerTag = "v1.2.1"
  $PatchRegistry = "docker.io"
  $PatchImage = "ingress-nginx/kube-webhook-certgen"
  $PatchTag = "v1.2.0"
  $DefaultBackendRegistry = "k8s.gcr.io"
  $DefaultBackendImage = "defaultbackend-amd64"
  $DefaultBackendTag = "1.5"

  # cert-manager
  $CertManagerRegistry = "quay.io"
  $CertManagerTag = "v1.8.2"
  $CertManagerImageController = "jetstack/cert-manager-controller"
  $CertManagerImageWebhook = "jetstack/cert-manager-webhook"
  $CertManagerImageCaInjector = "jetstack/cert-manager-cainjector"

  if (!$RegistryName) { throw "RegistryName is not set"}

  # nginx-ingress-controller
  # https://docs.microsoft.com/en-us/azure/aks/ingress-basic?tabs=azure-cli
  az acr import --name $RegistryName --source $ControllerRegistry/$ControllerImage`:$ControllerTag --image $ControllerImage`:$ControllerTag
  az acr import --name $RegistryName --source $ControllerRegistry/$PatchImage`:$PatchTag --image $PatchImage`:$PatchTag
  az acr import --name $RegistryName --source $ControllerRegistry/$DefaultBackendImage`:$DefaultBackendTag --image $DefaultBackendImage`:$DefaultBackendTag

  # cert-manager
  # https://docs.microsoft.com/en-us/azure/aks/ingress-tls?tabs=azure-cli
  az acr import --name $RegistryName --source $CertManagerRegistry/$CertManagerImageController`:$CertManagerTag --image $CertManagerImageController`:$CertManagerTag
  az acr import --name $RegistryName --source $CertManagerRegistry/$CertManagerImageWebhook`:$CertManagerTag --image $CertManagerImageWebhook`:$CertManagerTag
  az acr import --name $RegistryName --source $CertManagerRegistry/$CertManagerImageCaInjector`:$CertManagerTag --image $CertManagerImageCaInjector`:$CertManagerTag
}
finally {
  Pop-Location
}
