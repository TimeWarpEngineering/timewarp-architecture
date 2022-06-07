To use local images for Kubernetes we will run a local registry in docker iteself

docker run -d -p 9443:5000 --restart always --name registry registry:2
