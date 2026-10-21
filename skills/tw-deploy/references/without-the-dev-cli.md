# Deploying without the dev CLI

The `dev` CLI lives in the template's source repository. Without it, run the same commands
the verbs wrap, and do the preflight yourself (`helm version`, `kubectl config current-context`,
`kubectl get --raw /version`):

```bash
aspire deploy  --apphost <apphost.csproj> --environment Production -- --Publish:Target=<target> --Parameters:<name>=<value>
aspire destroy --apphost <apphost.csproj> --environment Production -- --Publish:Target=<target> [--Parameters:<name>=<value>]
```

When `aspire destroy` fails or reports nothing to destroy, nothing was removed; use the manual
removal in the `dev deprovision` notes of the tw-deploy skill. `dev deploy migrate` runs the commands in the skill's Postgres and migrations table, and
`dev open` runs `kubectl port-forward --namespace ingress-nginx
service/ingress-nginx-controller <ingress-port>:80` (kind) and opens
`http://localhost:<ingress-port>`, where `<ingress-port>` is the `Parameters:ingress-port` value
in the AppHost `appsettings.json`.
