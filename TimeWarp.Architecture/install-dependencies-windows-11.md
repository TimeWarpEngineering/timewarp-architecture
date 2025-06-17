## See if already installed

winget list pwsh
winget list Microsoft.DotNet.SDK.6
winget list CoreyButler.NVMforWindows
winget list Microsoft.AzureCosmosEmulator
winget list Microsoft.AzureCLI
wsl --list
winget list Docker.DockerDesktop

## Install commands

winget install pwsh
winget install Microsoft.DotNet.SDK.6
winget install CoreyButler.NVMforWindows
winget install Microsoft.AzureCosmosEmulator
winget install Microsoft.AzureCLI
wsl --install
winget install Docker.DockerDesktop
> Docker Desktop requires installing WSL2 see [install instructions for more details](https://docs.docker.com/desktop/windows/install/)
choco install kubernetes-helm (there currently isn't a winget option that I could find see issue https://github.com/helm/helm/issues/8203)


## Restore dotnet tools
dotnet tool restore
