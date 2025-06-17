
# Display variables
Write-Output "ApplicationNamespace = $ApplicationNamespace"

Push-Location $PSScriptRoot
try { 
  . ..\..\..\variables.ps1
  # Validate variables
  if (!$ApplicationNamespace) {  throw "ApplicationNamespace is not set";}
  if (!$DnsHostName) {  throw "DnsHostName is not set";}
  if (!$GrpcServerHostName) {  throw "GrpcServerHostName is not set";}

  Apply-Manifest ./web_ingress.yaml
  Apply-Manifest ./grpc_ingress.yaml
}
finally {
  Pop-Location
}
