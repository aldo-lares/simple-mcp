param location string = resourceGroup().location
param environmentName string
param resourceGroupName string = resourceGroup().name
param containerRegistryName string = ''
param kubernetesClusterName string = 'aks-simple-mcp'

// Variables
var abbreviations = {
  containerRegistry: 'acr'
  kubernetesCluster: 'aks'
  storageAccount: 'st'
  virtualNetwork: 'vnet'
  networkSecurityGroup: 'nsg'
  publicIPAddress: 'pip'
}

var resourceToken = uniqueString(resourceGroup().id)
var acrName = !empty(containerRegistryName) ? containerRegistryName : '${abbreviations.containerRegistry}${replace(environmentName, '-', '')}${resourceToken}'
var aksName = kubernetesClusterName
var vnetName = '${abbreviations.virtualNetwork}-${environmentName}'
var nsgName = '${abbreviations.networkSecurityGroup}-${environmentName}'
var storageName = '${abbreviations.storageAccount}${replace(environmentName, '-', '')}${resourceToken}'

// ACR - Azure Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

// Virtual Network
resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/8'
      ]
    }
    subnets: [
      {
        name: 'aks-subnet'
        properties: {
          addressPrefix: '10.240.0.0/16'
          networkSecurityGroup: {
            id: nsg.id
          }
          serviceEndpoints: [
            {
              service: 'Microsoft.ContainerRegistry'
            }
          ]
        }
      }
    ]
  }
}

// Network Security Group
resource nsg 'Microsoft.Network/networkSecurityGroups@2023-11-01' = {
  name: nsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'allow-http'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '5057'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
    ]
  }
}

// Storage Account for logs
resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
  }
}

// Managed Identity for AKS cluster control plane
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-aks-${environmentName}'
  location: location
}

// Managed Identity for AKS - kubelet
resource kubeletIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-aks-kubelet-${environmentName}'
  location: location
}

// Role Assignment: Managed Identity Operator for AKS control plane
// MUST happen before AKS cluster creation
resource managedIdentityOperatorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kubeletIdentity
  name: guid(kubeletIdentity.id, managedIdentity.id, 'mio')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f1a07417-d97a-45cb-824c-7a7467783830')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// AKS Cluster
// Depends on managedIdentityOperatorRoleAssignment to have permission to assign kubelet identity
resource aksCluster 'Microsoft.ContainerService/managedClusters@2024-01-01' = {
  dependsOn: [managedIdentityOperatorRoleAssignment]
  name: aksName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    kubernetesVersion: '1.34'
    dnsPrefix: '${aksName}-dns'
    enableRBAC: true
    agentPoolProfiles: [
      {
        name: 'systempool'
        count: 2
        vmSize: 'Standard_D2s_v3'
        osType: 'Linux'
        mode: 'System'
        vnetSubnetID: '${vnet.id}/subnets/aks-subnet'
        nodeTaints: []
        maxPods: 110
      }
    ]
    networkProfile: {
      networkPlugin: 'azure'
      networkPolicy: 'azure'
      serviceCidr: '10.0.0.0/16'
      dnsServiceIP: '10.0.0.10'
      outboundType: 'loadBalancer'
    }
    apiServerAccessProfile: {
      enablePrivateCluster: false
    }
    addonProfiles: {
      httpApplicationRouting: {
        enabled: false
      }
      omsagent: {
        enabled: false
      }
    }
    identityProfile: {
      kubeletidentity: {
        resourceId: kubeletIdentity.id
        clientId: kubeletIdentity.properties.clientId
        objectId: kubeletIdentity.properties.principalId
      }
    }
  }
}

// Role Assignment: ACR Pull for Kubelet Identity
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(containerRegistry.id, kubeletIdentity.id, 'acrpull')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: kubeletIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output registryLoginServer string = containerRegistry.properties.loginServer
output registryName string = containerRegistry.name
output clusterName string = aksCluster.name
output clusterId string = aksCluster.id
output resourceGroupName string = resourceGroupName
output location string = location
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityId string = managedIdentity.id
output kubeletIdentityClientId string = kubeletIdentity.properties.clientId
output kubeletIdentityPrincipalId string = kubeletIdentity.properties.principalId
