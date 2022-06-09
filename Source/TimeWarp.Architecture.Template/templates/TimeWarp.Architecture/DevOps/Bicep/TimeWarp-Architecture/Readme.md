# Deploy the Azure Resource Group 

Singapore Data Cetner doesn't seem to support CosmosDB (WHY NOT?) So will use Japan East

```Powershell
$Location = "japaneast"
```

## With Powershell

Set your subscription
Set-AzContext -Subscription $SubscriptionId

```Powershell
New-AzSubscriptionDeployment `
  -Name timewarpSubDeployment `
  -Location $Location `
  -TemplateFile subscription.bicep `
  -rgName timewarp-rg `
  -rgLocation $Location
```

## With Az Cli

```Powershell
az deployment sub create `
  --name timewarpSubDeployment `
  --location $Location `
  --template-file subscription.bicep `
  --parameters rgName=timewarp-rg rgLocation=$Location
```

# Deploy resources to the TimeWarp Resource Group

```Powershell
New-AzDeployment `
  -Name timewarpDeployment `
  -Location $Location `
  -TemplateFile main.bicep `
```

```Powershell
az configure --defaults group=timewarp-rg

az deployment group create `
  --template-file main.bicep
```
