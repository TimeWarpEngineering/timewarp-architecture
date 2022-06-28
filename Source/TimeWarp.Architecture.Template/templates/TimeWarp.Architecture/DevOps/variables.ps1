# Bicep
$BaseName = "timewarp" # 'Prefix for all resources, i.e. {basename}storage')
$Location = "japaneast"
$SubscriptionName = "StevenTCramer"
$AppConfigName = "$($BaseName)appconfig"
$ClusterName = "$BaseName-aks"
$ResourceGroupName = "$BaseName-rg"
$RegistryName = "$($BaseName)acr"

# Kubernetes

$RegistryHost = "$($RegistryName).azurecr.io"
$AspNetCore_Environment = "Development"

# servers
$ApiServerImageTag = "1.0.0"
$WebServerImageTag = "1.0.0"
$GrpcServerImageTag = "1.0.0"
$YarpServerImageTag = "1.0.0"

$ApplicationNamespace = $BaseName
# $global:Yarp_InsecurePort=80
# $global:Yarp_SecurePort=443
# $global:WebServer_InsecurePort=5200
# $global:WebServer_SecurePort=7200
# $global:ApiServer_InsecurePort=5201
# $global:GrpcServer_InsecurePort=5202
# $global:AspNetCore_Environment = "Kubernetes_Docker"
