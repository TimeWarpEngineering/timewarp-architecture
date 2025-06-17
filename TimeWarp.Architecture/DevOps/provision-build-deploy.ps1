
# Provision via Bicep
# Build docker images
# Deploy to Kubernetes

Push-Location $PSScriptRoot
try {   

  # load variables
  . "$PSScriptRoot\variables.ps1"

  .\Bicep\provision.ps1
  .\Docker\BuildImages.ps1
  .\Kubernetes\deploy.ps1
}
finally {
  Pop-Location
}
