param basename string
param location string

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
}
