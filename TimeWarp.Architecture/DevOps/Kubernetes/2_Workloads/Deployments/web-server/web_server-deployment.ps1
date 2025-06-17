Push-Location $PSScriptRoot
try {   
  # load variables
  . ..\..\..\..\variables.ps1
  # ensure required variables are set
  if (!$BaseName) { throw "BaseName is not set" }
  
  $ConnectionStrings__AppConfig = (Get-AzAppConfigurationStoreKey -Name $AppConfigName -ResourceGroupName $ResourceGroupName | Where-Object Name -EQ "Primary Read Only").ConnectionString
  Deploy-Server `
    -file ./web_server-deployment.yaml `
    -name "web-server" `
    -imageTag $WebServerImageTag `
    -cluster $ClusterName `
    -namespace $ApplicationNamespace `
    -environment $AspNetCore_Environment `
    -registryHost $RegistryHost `
    -appConfigConnectionString $ConnectionStrings__AppConfig
}
finally {
  Pop-Location
}
