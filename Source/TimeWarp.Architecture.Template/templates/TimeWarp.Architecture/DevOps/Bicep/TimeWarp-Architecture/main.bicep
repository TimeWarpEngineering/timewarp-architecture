param basename string
param location string = 'japaneast'

@minLength(1)
param principalId string

var secrets = [
  'get'
  'set'
  'list'
  'delete'
]


module key_vault 'modules/key_vault.bicep' = {
  name: '${basename}-keyvault'
  params: {
    basename: basename
    location: location
  }
}

module cosmos_db 'modules/cosmos_db.bicep' = {
  name: '${basename}-cosmosdb'
  params: {
    basename: basename
    location: location
  }
}



resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: '${basename}acr'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    adminUserEnabled: true
  }
}

resource key_vault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: '${basename}-kv'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        objectId: principalId
        permissions: {
          secrets: secrets
        }
        tenantId: subscription().tenantId
      }
      // {
      //   objectId: aks.properties.identityProfile.kubeletidentity.objectId
      //   permissions: {
      //     secrets: secrets
      //   }
      //   tenantId: subscription().tenantId
      // }
      // {
      //   objectId: aks.identity.principalId
      //   permissions: {
      //     secrets: secrets
      //   }
      //   tenantId: subscription().tenantId
      // }
      // {
      //   objectId: function.identity.principalId
      //   permissions: {
      //     secrets: secrets
      //   }
      //   tenantId: subscription().tenantId
      // }
    ]
  }

  resource cosmos_primaryMasterKey_secret 'secrets' = {
    name: 'CosmosDbOptions--AccessKey'
    properties: {
      value: cosmos_db.outputs.primaryMasterKey
    }
  }

  resource cosmos_primaryConnectionString_secret 'secrets' = {
    name: 'CosmosPrimaryConnectionString'
    properties: {
      value: cosmos_db.outputs.primaryConnectionString
    }
  }
}

// {"uri":"https://cramer-test-key-vault.vault.azure.net/secrets/KeyServiceOptions--EncryptedAppTitle"}
var cosmosDbOptions_AccessKey_keyVaultRef = {
  uri: '${key_vault.properties.vaultUri}/sercrets/CosmosDbOptions--AccessKey'
}


resource appconfig 'Microsoft.AppConfiguration/configurationStores@2022-05-01' = {
  name: '${basename}appconfig'
  location: location
  sku: {
    name: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }

  resource sentinel_keyValutSecret 'keyValues@2022-05-01' = {
    name: 'Sentinel'
    properties: {
      value: '0'
    }
  }

  resource cosmosDbOptions_AccessKey_keyValutSecret 'keyValues@2022-05-01' = {
    name: 'CosmosDbOptions:AccessKey'
    properties: {
      value: string(cosmosDbOptions_AccessKey_keyVaultRef)
      contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
    }
  }
}
