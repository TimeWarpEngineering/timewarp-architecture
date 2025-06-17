# Provision the Azure Resources

Update your desired settings in `DevOps\variables.ps1` and then execute:

`DevOps\Bicep\TimeWarp-Architecture\provision.ps1`


Singapore Data Center doesn't seem to support CosmosDB (WHY NOT?) So will use Japan East

```Powershell
$Location = "japaneast"
```

## Provision the Azure Resource Group 


### With Powershell

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

# References

Memealyzer
