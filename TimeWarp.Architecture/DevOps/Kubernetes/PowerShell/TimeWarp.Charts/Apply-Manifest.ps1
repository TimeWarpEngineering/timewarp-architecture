function Apply-Manifest
{
  <#
    .SYNOPSIS 
      Will Expand the given file with Powershell variables and use kubectl to apply the result
    .PARAMETER file
      Specifies the path to the manifest file to expand
    .PARAMETER cluster
      Specifies the kubernetes cluster to use.
    .PARAMETER namespace
      Specifies the namespace in the cluster to use.
    .EXAMPLE 
      PS> Apply-Manifest somefile.yaml
  #>
  [CmdletBinding()]
  param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$file=$(throw "file is mandatory, please provide a value."),
  
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$cluster=$(throw "cluster is mandatory, please provide a value."),
  
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$namespace=$(throw "namespace is mandatory, please provide a value.")
  )

  if (!(Test-Path $file))
  {
    throw "File not found: $file"
  }
  $template = Get-Content $file -Raw
  $expanded = $ExecutionContext.InvokeCommand.ExpandString($template)
  Write-Host "==========================================================="
  Write-Host "Expanded manifest ($file)"
  Write-Host "===========================================================`n"
  $expanded
  Write-Host "===========================================================`n"
  kubectl config set-context $cluster --namespace $namespace
  $expanded | kubectl apply -f -
  Write-Host "-----------------------------------------------------------`n"
}
