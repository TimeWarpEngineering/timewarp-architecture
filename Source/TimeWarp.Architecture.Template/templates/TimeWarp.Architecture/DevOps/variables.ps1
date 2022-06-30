# Variables used for DevOps scripts
$BaseName = "timewarp" # 'Prefix for all resources, i.e. {basename}storage')
$Location = "japaneast" # Set your prefered azure location
$SubscriptionName = "StevenTCramer" # Set your subscription name

# Bicep
$AppConfigName = "$($BaseName)appconfig"
$ClusterName = "$BaseName-aks"
$RegistryName = "$($BaseName)acr"
$ResourceGroupName = "$BaseName-rg"
$KeyVaultName = "$($BaseName)-kv"

# Kubernetes

$RegistryHost = "$($RegistryName).azurecr.io"
$AspNetCore_Environment = "Development"

# servers
$ApiServerImageTag = "1.0.0"
$WebServerImageTag = "1.0.0"
$GrpcServerImageTag = "1.0.0"
$YarpServerImageTag = "1.0.0"

$ApplicationNamespace = $BaseName
