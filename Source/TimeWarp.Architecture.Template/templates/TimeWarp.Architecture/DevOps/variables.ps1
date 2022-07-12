# Variables used for DevOps scripts
$BaseName = "timewarp" # 'Prefix for all resources, i.e. {basename}storage')

# Set your prefered azure location 
# use `az account list-locations -o table ` to get a list of available locations
# `centralindia` tends to be the cheapest
$Location = "centralus" 
$SubscriptionName = "StevenTCramer" # Set your subscription name

# Bicep
$AppConfigName = "$($BaseName)appconfig"
$ClusterName = "$BaseName-aks"
$RegistryName = "$($BaseName)acr"
$ResourceGroupName = "$BaseName-rg"
$KeyVaultName = "$($BaseName)-kv"
$DnsHostName =  "$BaseName-cluster.$Location.cloudapp.azure.com"
$GrpcServerHostName = "$BaseName-grpc-server.$Location.cloudapp.azure.com"

# Kubernetes
$RegistryHost = "$($RegistryName).azurecr.io"
$AspNetCore_Environment = "Development"

# servers
$ApiServerImageTag = "1.0.0"
$WebServerImageTag = "1.0.0"
$GrpcServerImageTag = "1.0.0"
$YarpServerImageTag = "1.0.0"

$ApplicationNamespace = $BaseName
