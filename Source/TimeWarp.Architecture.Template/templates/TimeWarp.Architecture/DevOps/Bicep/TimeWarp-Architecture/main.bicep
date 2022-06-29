param basename string
param appconfigname string
param clustername string
param location string

@minLength(1)
param principalId string

var secrets = [
  'get'
  'set'
  'list'
  'delete'
]

var accountName = '${basename}-cosmos-account'
var databaseName = basename

resource cosmos_account 'Microsoft.DocumentDB/databaseAccounts@2021-10-15' = {
  name: toLower(accountName)
  location: location
  properties: {
    enableFreeTier: true
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
      }
    ]
  }

  resource cosmos_db 'sqlDatabases' = {
    name: databaseName
    properties: {
      resource: {
        id: databaseName
      }
      options: {
        throughput: 400
      }
    }
  }
}

resource container_registry 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
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
      {
        objectId: aks.properties.identityProfile.kubeletidentity.objectId
        permissions: {
          secrets: secrets
        }
        tenantId: subscription().tenantId
      }
      {
        objectId: aks.identity.principalId
        permissions: {
          secrets: secrets
        }
        tenantId: subscription().tenantId
      }
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
      value: cosmos_account.listKeys().primaryMasterKey
    }
  }
}

var cosmosDbOptions_AccessKey_keyVaultRef = {
  uri: '${key_vault.properties.vaultUri}secrets/CosmosDbOptions--AccessKey'
}

resource app_config 'Microsoft.AppConfiguration/configurationStores@2022-05-01' = {
  name: appconfigname
  location: location
  sku: {
    name: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }

  resource sentinel_setting 'keyValues@2022-05-01' = {
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

  resource cosmosDbOptions_Endpoint_setting 'keyValues@2022-05-01' = {
    name: 'CosmosDbOptions:Endpoint'
    properties: {
      value: cosmos_account.properties.documentEndpoint
    }
  }
}

resource aks 'Microsoft.ContainerService/managedClusters@2020-09-01' = {
  name: clustername
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    kubernetesVersion: '1.23.5'
    nodeResourceGroup: '${clustername}nodes'
    dnsPrefix: clustername

    agentPoolProfiles: [
      {
        name: 'default'
        count: 1
        vmSize: 'Standard_DS2_v2'
        mode: 'System'
      }
    ]
  }
}

resource application_insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${basename}ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

module cli_perms './Modules/Authorization/rolesapp.bicep' = {
  name: 'cli_perms-${resourceGroup().name}'
  params: {
    principalId: principalId
    principalType: 'User'
    resourceGroupName: resourceGroup().name
  }
}

module aks_kubelet_perms './Modules/Authorization/rolesapp.bicep' = {
  name: 'aks_kubelet_perms-${resourceGroup().name}'
  params: {
    principalId: aks.properties.identityProfile.kubeletidentity.objectId
    resourceGroupName: resourceGroup().name
  }
}
module aks_cluster_perms './Modules/Authorization/rolesacr.bicep' = {
  name: 'aks_cluster_perms-${resourceGroup().name}'
  params: {
    principalId: aks.identity.principalId
    resourceGroupName: resourceGroup().name
  }
}

output app_config_connectionstring string = app_config.listKeys().value[0].connectionString
