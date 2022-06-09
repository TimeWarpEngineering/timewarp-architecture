param location string = 'japaneast'
param cosmosDatabaseName string = 'timewarp'

module cosmos_db 'modules/cosmos_db.bicep' = {
  name: 'timewarp-cosmosdb'
  params: {
    location: location
    databaseName: cosmosDatabaseName
  }
}
