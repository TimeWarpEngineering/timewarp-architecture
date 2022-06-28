targetScope = 'subscription'

@minLength(1)
@maxLength(17)
@description('Prefix for all resources, i.e. {basename}storage')
param basename string

@description('Primary location for all resources')
param location string = 'japaneast'

@minLength(1)
param principalId string

@minLength(1)
param appconfigname string

@minLength(1)
param clustername string

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name:'${basename}-rg'
  location: location
}

module main 'main.bicep' = {
  name: 'main'
  scope: rg
  params: {
    basename: basename
    location: location
    principalId: principalId
    appconfigname: appconfigname
    clustername: clustername
  }
}

output app_config_connectionstring string = main.outputs.app_config_connectionstring
output azure_client_id string = main.outputs.azure_client_id
output azure_client_secret string = main.outputs.azure_client_secret
output azure_tenant_id string = main.outputs.azure_tenant_id
