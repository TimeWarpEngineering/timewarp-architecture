Push-Location $PSScriptRoot/../..
try { 
  # source variables
  . "$PSScriptRoot\..\variables.ps1"
  if (!$RegistryName) { throw "RegistryName is not set"}
  if (!$RegistryHost) { throw "RegistryHost is not set"}
  az acr login --name $RegistryName
  docker build --progress=plain -f .\Source\ContainerApps\Web\Web.Server\Dockerfile -t $RegistryHost/web-server:1.0.0 .
  docker build --progress=plain -f .\Source\ContainerApps\Api\Api.Server\Dockerfile -t $RegistryHost/api-server:1.0.0 .
  docker build --progress=plain -f .\Source\ContainerApps\Grpc\Grpc.Server\Dockerfile -t $RegistryHost/grpc-server:1.0.0 .
  docker build --progress=plain -f .\Source\ContainerApps\Yarp\Dockerfile -t $RegistryHost/yarp:1.0.0 .
  
  docker push $RegistryHost/web-server:1.0.0
  docker push $RegistryHost/api-server:1.0.0
  docker push $RegistryHost/grpc-server:1.0.0
  docker push $RegistryHost/yarp:1.0.0  
}
finally {
  Pop-Location
}
  