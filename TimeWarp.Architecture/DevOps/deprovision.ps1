Push-Location $PSScriptRoot
try {   
  .\Bicep\deprovision.ps1
}
finally {
  Pop-Location
}
