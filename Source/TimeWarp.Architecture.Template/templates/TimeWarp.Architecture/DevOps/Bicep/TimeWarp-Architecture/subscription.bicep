targetScope = 'subscription'

param rgLocation string = 'japaneast'
param rgName string = 'timewarp-rg'
resource rg 'Microsoft.Resources/resourceGroups@2021-01-01' = {
  name: rgName
  location: rgLocation
}
