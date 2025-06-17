targetScope = 'subscription'

@minLength(3)
@maxLength(17)
@description('Prefix for all resources, i.e. {basename}storage')
param basename string

@description('Primary location for all resources')
param location string = 'japaneast'

@minLength(3)
param principalId string

@minLength(3)
param appconfigname string

@minLength(3)
param clustername string

@minLength(3)
param keyvaultname string

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
    keyvaultname: keyvaultname
  }
}

output app_config_connectionstring string = main.outputs.app_config_connectionstring
