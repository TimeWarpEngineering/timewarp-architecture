
# Provision via Bicep
# Build docker images
# Deploy to Kubernetes

Push-Location $PSScriptRoot
try {   
  .\Bicep\TimeWarp-Architecture\provision.ps1
  .\Docker\BuildImages.ps1
  .\Kubernetes\deploy.ps1
}
finally {
  Pop-Location
}
