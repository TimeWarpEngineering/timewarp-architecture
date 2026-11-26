# Deploy on release (GitHub Actions)

A merge never deploys. A published GitHub release does, and so does a manual dispatch that asks
for it. The generated app's `.github/workflows/workflow.yml` has a `deploy` job for that. It runs
the same `dev deploy --target <t>` an operator runs: publish, `aspire deploy`, migrate when the
app has a database, smoke. It does not repeat those steps in YAML.

| Event | What runs |
|-------|-----------|
| push, pull request | CI (`classify`, `ci-group`, `ci`, `green-key`). The `deploy` job is skipped. |
| `release` (`published`) | `deploy` only. The release's commit passed CI on its pull request. |
| `workflow_dispatch`, `deploy` off | CI, a full run. |
| `workflow_dispatch`, `deploy` on | `deploy` only, on the ref you dispatch (a branch or a tag). |

The job:

- runs on `ubuntu-latest` (`aspire deploy` builds the images with Docker) in the GitHub
  environment `Production`, one deploy at a time (concurrency group `deploy-production`, a new
  deploy waits and does not cancel one in progress);
- installs .NET and the pinned Aspire CLI through `.github/actions/setup-ci` (`aspire-cli: 'true'`);
- logs in with `azure/login` over GitHub OIDC (`id-token: write`). There is no client secret;
- runs `dev deploy --target $DEPLOY_TARGET --yes --no-browser`. `--yes` because nobody can answer a
  prompt, `--no-browser` prints the URL.

Targets are `aca` and `aca-neon`. Compose and Kubernetes deploy from an operator's machine.

`dev template-smoke` and dev-cli-tests check this job (`AppCiWorkflowShape`): it must stay
release- and dispatch-only, in `Production`, on OIDC, with one `dev deploy` line. No other job may
deploy, migrate a deployment, deprovision or open.

## Configuration

Repository (or `Production` environment) variables and secrets. The job reads them into
environment variables, never onto the command line. An unset one is left out, not set empty.

| Name | Kind | Value |
|------|------|-------|
| `AZURE_CLIENT_ID` | variable | the app registration's application (client) id |
| `AZURE_TENANT_ID` | variable | the tenant id |
| `AZURE_SUBSCRIPTION_ID` | variable | the subscription; becomes `Azure__SubscriptionId` |
| `AZURE_LOCATION` | variable | for example `eastus2`; becomes `Azure__Location`. Put it near the Neon region |
| `AZURE_RESOURCE_GROUP` | variable | becomes `Azure__ResourceGroup` |
| `DEPLOY_TARGET` | variable, optional | `aca` or `aca-neon`. Unset means `aca-neon` |
| `PASSKEY_RP_HOST` | variable, optional | a custom domain for passkeys; becomes `Parameters__passkey-rp-host` ([passkey-hosts.md](passkey-hosts.md)) |
| `NEON_POOLED_CONNECTION_STRING` | secret, aca-neon | becomes `Parameters__neon-pooled-connection-string` |
| `NEON_DIRECT_CONNECTION_STRING` | secret, aca-neon | becomes `Parameters__neon-direct-connection-string` |

The step refuses a missing Azure variable by name. `dev deploy` refuses a missing Neon string by
name. GitHub masks secret values in the log, and `dev deploy` never prints them.

## One-time setup

### Azure: app registration and federated credential

Run once, logged in as someone who can create app registrations and assign roles on the
subscription. Replace `<owner>/<repo>` with the GitHub repository and `<subscription-id>` with the
subscription.

```bash
app_id=$(az ad app create --display-name "<repo>-github-deploy" --query appId -o tsv)
sp_id=$(az ad sp create --id "$app_id" --query id -o tsv)

az ad app federated-credential create --id "$app_id" --parameters '{
  "name": "github-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>/<repo>:environment:Production",
  "audiences": ["api://AzureADTokenExchange"]
}'

for role in "Contributor" "User Access Administrator"; do
  az role assignment create --assignee-object-id "$sp_id" --assignee-principal-type ServicePrincipal \
    --role "$role" --scope "/subscriptions/<subscription-id>"
done
```

Contributor creates the resources. `aspire deploy` also writes role assignments (the registry pull
and Key Vault roles of the apps' managed identity), which Contributor cannot do. User Access
Administrator, or the narrower Role Based Access Control Administrator, grants that.

**The subject is `repo:<owner>/<repo>:environment:Production`.** A job that names an environment
gets a token whose subject is that environment, whatever the ref. So one credential covers the
release run (its ref is `refs/tags/<tag>`) and a dispatch from any branch or tag. A credential
that trusts `repo:<owner>/<repo>:ref:refs/heads/master` refuses this job, on a release and on a
dispatch alike. The refusal is `AADSTS700213: No matching federated identity record found`.

### GitHub: environment, variables, secrets

```bash
gh api -X PUT "repos/<owner>/<repo>/environments/Production"

gh variable set AZURE_CLIENT_ID --body "$app_id"
gh variable set AZURE_TENANT_ID --body "$(az account show --query tenantId -o tsv)"
gh variable set AZURE_SUBSCRIPTION_ID --body "<subscription-id>"
gh variable set AZURE_LOCATION --body "eastus2"
gh variable set AZURE_RESOURCE_GROUP --body "<repo>-rg"
gh variable set DEPLOY_TARGET --body "aca-neon"           # optional; aca-neon is the default

gh secret set NEON_POOLED_CONNECTION_STRING --env Production   # prompts; the value stays off the command line
gh secret set NEON_DIRECT_CONNECTION_STRING --env Production
```

To gate each deploy, add required reviewers to the `Production` environment (Settings,
Environments). A deployment branch and tag rule there limits which refs may deploy.

Environments on a **private** repository need GitHub Pro, Team or Enterprise; on a public
repository every plan has them. Without environments there is no `environment:Production` subject,
and this job cannot log in as written.

## Releasing

Publish a release (`gh release create v1.2.0 --generate-notes`, or the web UI). A draft does not
deploy until it is published; a pre-release deploys when it is published. A release created by a
workflow with `GITHUB_TOKEN` starts no workflow run, so it does not deploy: publish it as a user or
a GitHub App.

Re-run a failed deploy with **Re-run jobs** on that run, or dispatch:

```bash
gh workflow run workflow.yml --ref v1.2.0 -f deploy=true
```

The dev CLI has no command for the one-time setup; it is the steps above.
