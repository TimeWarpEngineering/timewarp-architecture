# Local Kubernetes with kind

Detail for the `tw-deploy` skill ([SKILL.md](../SKILL.md)).

A kind cluster is just another kubectl context. Give it a local registry the nodes can pull from,
install ingress-nginx once as cluster infrastructure, then deploy, migrate and open. (kind also
runs on Podman with `KIND_EXPERIMENTAL_PROVIDER=podman`; substitute `podman` for `docker` below.)

pwsh:

```powershell
# 1. Local registry, reachable from the host as localhost:5001
docker run -d --restart=always -p 127.0.0.1:5001:5000 --network bridge --name kind-registry registry:2

# 2. Cluster whose containerd reads per-registry config
@'
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
containerdConfigPatches:
- |-
  [plugins."io.containerd.grpc.v1.cri".registry]
    config_path = "/etc/containerd/certs.d"
'@ | kind create cluster --name app --config=-

# 3. Map localhost:5001 inside every node to the registry container, and join the networks
foreach ($node in kind get nodes --name app) {
  docker exec $node mkdir -p /etc/containerd/certs.d/localhost:5001
  '[host."http://kind-registry:5000"]' | docker exec -i $node cp /dev/stdin /etc/containerd/certs.d/localhost:5001/hosts.toml
}
docker network connect kind kind-registry

# 4. Ingress controller — cluster infrastructure, installed once, never by the app chart
helm upgrade --install ingress-nginx ingress-nginx `
  --repo https://kubernetes.github.io/ingress-nginx --namespace ingress-nginx --create-namespace
kubectl wait --namespace ingress-nginx --for=condition=ready pod `
  --selector=app.kubernetes.io/component=controller --timeout=180s

# 5. Deploy (kubectl context is now kind-app); the committed AppHost appsettings.json already
#    names the namespace, release and repository after the app and points at localhost:5001.
#    Preflight checks the cluster answers, kind lists it and localhost:5001 is up
dev deploy --target kubernetes

# 6. Migrate (dev publish kubernetes first if artifacts/aspire-output/kubernetes is missing),
#    then reach the ingress: forwards localhost:8080 and opens the browser; Ctrl+C stops it
dev deploy migrate --target kubernetes
dev open --target kubernetes

# Tear down the app (data included), then the cluster and registry when done
dev deprovision --target kubernetes --yes
kind delete cluster --name app; docker rm -f kind-registry
```

bash:

```bash
# 1. Local registry, reachable from the host as localhost:5001
docker run -d --restart=always -p 127.0.0.1:5001:5000 --network bridge --name kind-registry registry:2

# 2. Cluster whose containerd reads per-registry config
cat <<'YAML' | kind create cluster --name app --config=-
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
containerdConfigPatches:
- |-
  [plugins."io.containerd.grpc.v1.cri".registry]
    config_path = "/etc/containerd/certs.d"
YAML

# 3. Map localhost:5001 inside every node to the registry container, and join the networks
for node in $(kind get nodes --name app); do
  docker exec "$node" mkdir -p /etc/containerd/certs.d/localhost:5001
  printf '[host."http://kind-registry:5000"]\n' | docker exec -i "$node" cp /dev/stdin /etc/containerd/certs.d/localhost:5001/hosts.toml
done
docker network connect kind kind-registry

# 4. Ingress controller — cluster infrastructure, installed once, never by the app chart
helm upgrade --install ingress-nginx ingress-nginx \
  --repo https://kubernetes.github.io/ingress-nginx --namespace ingress-nginx --create-namespace
kubectl wait --namespace ingress-nginx --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller --timeout=180s

# 5. Deploy (kubectl context is now kind-app); the committed AppHost appsettings.json already
#    names the namespace, release and repository after the app and points at localhost:5001.
#    Preflight checks the cluster answers, kind lists it and localhost:5001 is up
dev deploy --target kubernetes

# 6. Migrate, then reach the ingress (forwards localhost:8080, opens the browser; Ctrl+C stops it)
dev deploy migrate --target kubernetes
dev open --target kubernetes

# Tear down the app (data included), then the cluster and registry when done
dev deprovision --target kubernetes --yes
kind delete cluster --name app && docker rm -f kind-registry
```
