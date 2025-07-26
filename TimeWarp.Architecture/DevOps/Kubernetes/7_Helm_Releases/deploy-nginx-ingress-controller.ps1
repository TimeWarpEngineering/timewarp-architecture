# https://docs.microsoft.com/en-us/azure/aks/ingress-basic?tabs=azure-cli
# https://docs.microsoft.com/en-us/azure/aks/ingress-tls?tabs=azure-cli

Push-Location $PSScriptRoot
try {   
  . ".\import-helm-charts-to-acr.ps1"
  . "..\..\variables.ps1"

  if (!$ControllerImage) {  throw "ControllerImage is not set";}
  if (!$ControllerTag) {  throw "ControllerTag is not set";}
  if (!$PatchImage) {  throw "PatchImage is not set";}
  if (!$PatchTag) {  throw "PatchTag is not set";}
  if (!$DefaultBackendImage) {  throw "DefaultBackendImage is not set";}
  if (!$DefaultBackendTag) {  throw "DefaultBackendTag is not set";}
  if (!$RegistryName) {  throw "RegistryName is not set";}
  if (!$ResourceGroupName) {  throw "ResourceGroupName is not set";}

  $AcrUrl = (Get-AzContainerRegistry -ResourceGroupName $ResourceGroupName -Name $RegistryName).LoginServer
  $StaticIp = (Get-AzPublicIpAddress -ResourceGroupName $ResourceGroupName).IpAddress
  $DnsLabel = "$BaseName-aks-ingress"
  # <LABEL>.<AZURE REGION NAME>.cloudapp.azure.com
  # example: timewarp-aks-ingress.centralus.cloudapp.azure.com

  helm upgrade --install nginx-ingress ingress-nginx/ingress-nginx `
      --namespace $BaseName --create-namespace `
      --set controller.replicaCount=2 `
      --set controller.nodeSelector."kubernetes\.io/os"=linux `
      --set controller.image.registry=$AcrUrl `
      --set controller.image.image=$ControllerImage `
      --set controller.image.tag=$ControllerTag `
      --set controller.image.digest="" `
      --set controller.admissionWebhooks.patch.nodeSelector."kubernetes\.io/os"=linux `
      --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz `
      --set controller.admissionWebhooks.patch.image.registry=$AcrUrl `
      --set controller.admissionWebhooks.patch.image.image=$PatchImage `
      --set controller.admissionWebhooks.patch.image.tag=$PatchTag `
      --set controller.admissionWebhooks.patch.image.digest="" `
      --set defaultBackend.nodeSelector."kubernetes\.io/os"=linux `
      --set defaultBackend.image.registry=$AcrUrl, `
      --set defaultBackend.image.image=$DefaultBackendImage `
      --set defaultBackend.image.tag=$DefaultBackendTag `
      --set defaultBackend.image.digest="" `
      --set controller.service.loadBalancerIP=$StaticIp `
      --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-dns-label-name"=$DnsLabel `
      --debug
}
finally {
  Pop-Location
}
