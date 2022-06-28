function Deploy-Server
{
  <#
    .SYNOPSIS
    Deploys the server to the namespace in the kubernetes cluster with given name and image tag
    .PARAMETER file
      Specifies the path to the manifest file to expand
    .PARAMETER cluster
      Specifies the kubernetes cluster to use.
    .PARAMETER namespace
      Specifies the namespace in the cluster to use.
    .PARAMETER environment
      Specifies the ASPNETCORE_ENVIRONMENT that will be set in the manifest.
    .PARAMETER name
      Specifies the name of the server to deploy.
    .PARAMETER registryHost
      Specifies the registry host to use.
    .PARAMETER imageTag
      Specifies the image tag to use.
  #>
  [CmdletBinding()]
  param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [ValidateScript({Test-Path $_ -Type Leaf})]
    [string]$file=$(throw "file is mandatory, please provide a value."),

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$name=$(throw "name is mandatory, please provide a value."),

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$namespace=$(throw "namespace is mandatory, please provide a value."),

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$registryHost=$(throw "registryHost is mandatory, please provide a value."),

    [Parameter()]
    [ValidateSet("Production","Development")]
    [string]$environment=$(throw "environment is mandatory, please provide a value."),

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$imageTag=$(throw "imageTag is mandatory, please provide a value."),
    
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$cluster=$(throw "cluster is mandatory, please provide a value.")
  )

  $script:ApplicationImage = "$registryHost/$($name):$imageTag"
  $script:ApplicationName = $name
  $script:ApplicationNamespace = $namespace
  $script:AspNetCore_Environment = $environment

  Apply-Manifest -file $file -cluster $ClusterName -namespace $ApplicationNameSpace 
}
