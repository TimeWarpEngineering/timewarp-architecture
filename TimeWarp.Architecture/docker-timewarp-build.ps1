$myRegistryName = "timewarpcontainerregistry"
$ImageVersion = "1.0.0"

az acr login --name $myRegistryName

docker --context default build --no-cache --progress=plain --force-rm -t timewarp-server:$ImageVersion -f ./DevOps/Docker/timewarp.software.dockerfile .
docker --context default tag timewarp-server:$ImageVersion timewarpcontainerregistry.azurecr.io/timewarp-server:$ImageVersion
docker --context default push timewarpcontainerregistry.azurecr.io/timewarp-server:$ImageVersion
