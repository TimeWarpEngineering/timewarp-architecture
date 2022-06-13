To use local images for Kubernetes we will run a local registry in docker iteself

docker run -d -p 9443:5000 --restart always --name registry registry:2

To build the docker images run the following commands from the sln directory.


```
docker build -f .\Source\ContainerApps\Web\Web.Server\Dockerfile -t localhost:9443/web-server:1.0.0 .
docker build -f .\Source\ContainerApps\Api\Api.Server\Dockerfile -t localhost:9443/api-server:1.0.0 .
docker build -f .\Source\ContainerApps\Grpc\Grpc.Server\Dockerfile -t localhost:9443/grpc-server:1.0.0 .
docker build -f .\Source\ContainerApps\Yarp\Dockerfile -t localhost:9443/yarp:1.0.0 .

docker push localhost:9443/web-server:1.0.0
docker push localhost:9443/api-server:1.0.0
docker push localhost:9443/grpc-server:1.0.0
docker push localhost:9443/yarp:1.0.0
```
