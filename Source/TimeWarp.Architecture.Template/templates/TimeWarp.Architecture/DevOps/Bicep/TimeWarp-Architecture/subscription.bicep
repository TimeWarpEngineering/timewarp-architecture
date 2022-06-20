targetScope = 'subscription'

@minLength(1)
@maxLength(17)
@description('Prefix for all resources, i.e. {basename}storage')
param basename string

@description('Primary location for all resources')
param location string = 'japaneast'

@minLength(1)
param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name:'${basename}-rg'
  location: location
}

module resources 'main.bicep' = {
  name: 'main'
  scope: rg
  params: {
    basename: basename
    location: location
    principalId: principalId
  }
}
