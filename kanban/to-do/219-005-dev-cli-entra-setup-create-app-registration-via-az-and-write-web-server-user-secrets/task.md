# Dev CLI entra setup: create app registration via az and write web-server user secrets

## Description

Add a `dev entra` command group to `tools/dev-cli` so a developer can go from "logged in to
`az`" to "Web.Server user secrets populated for the named `entra` scheme" with one command,
without copying ids/secrets through a portal or chat. Companion to 219-002 (scheme) and
219-004 (`PublicOrigin`). Idempotent: re-running reuses an existing app registration by
display name and only mints a new secret when asked.

## Parent

219

## Requirements

Commands (Nuru routes, `tools/dev-cli/endpoints/entra-*.cs`, same shape as `db-*` group):

- `dev entra setup [--name <display-name>] [--public-origin <https://host>] [--redirect-uri <uri>]... [--new-secret] [--dry-run]`
  1. Preflight: `az` on PATH; `az account show` succeeds (else print `az login` hint and exit 1).
     Print tenant id and signed-in user; prompt is not required (dev CLI is non-interactive).
  2. Find app by display name (`az ad app list --display-name`); if absent, `az ad app create
     --sign-in-audience AzureADMyOrg --web-redirect-uris ...`. Default display name
     `TimeWarp Architecture Dev`. Default redirect URIs: `https://localhost:63611/signin-oidc`,
     `https://localhost:63610/signin-oidc`, plus `{--public-origin}/signin-oidc` when given.
     If the app exists, **merge** redirect URIs (`az ad app update --web-redirect-uris` with the
     union) rather than overwrite.
  3. Ensure a service principal exists (`az ad sp show --id` / `az ad sp create --id`).
  4. Secret: if `--new-secret` or no secret was written before, `az ad app credential reset
     --append --display-name dev-<yyyyMMdd> --years 1`; capture the password in memory only.
     Never print it; never log the `az` command line containing it.
  5. Write user secrets on `source/container-apps/web/projects/web-server` via
     `dotnet user-secrets set` (project has `UserSecretsId`):
     `Authentication:Entra:Enabled=true`, `TenantId`, `ClientId`, `ClientSecret` (only when
     minted this run), `TrustedTenants:0=<tenant>`, `AllowBootstrap=true`, and
     `PublicOrigin` when `--public-origin` given (key added by 219-004; if 219-004 is not
     merged yet, still write the key — it is inert until the option exists).
  6. Print a summary table (tenant, appId, redirect URIs, which secrets were written, secret
     masked) and the next step (`dev run`, browse, click "Continue with Microsoft 365").
  `--dry-run` prints every `az` / `dotnet user-secrets` invocation without running them.
- `dev entra status`: shows current user-secret values for the section (secret masked), whether
  the app registration exists, and its redirect URIs.
- `dev entra disable`: sets `Authentication:Entra:Enabled=false` in user secrets (no Azure change).

Conventions: TimeWarp.Amuru `Shell.Builder("az")` / `CaptureAsync` (never
`System.Diagnostics.Process`), explicit types, `ITerminal` output, Purpose/Design regions,
handler stores Command/Ct as fields (see `build-command.cs`). No new NuGet packages. Tests:
dev-cli has no test project today — add a small Jaribu runfile test only if a seam is cheap
(e.g. redirect-URI union and summary masking as pure functions); otherwise document manual
validation.

## Checklist

- [ ] `entra-group.cs` + `entra-setup-command.cs`, `entra-status-command.cs`, `entra-disable-command.cs`
- [ ] Idempotent find-or-create app; redirect URI union; service principal ensure
- [ ] Secret minted only on first run / `--new-secret`; never echoed or logged
- [ ] User secrets written on web-server project; `--dry-run` prints without executing
- [ ] `dev --capabilities` lists the new group; region annotations pass `ganda repo audit`
- [ ] Docs: identity guide section "Local Entra setup" pointing at `dev entra setup`
- [ ] `dev build` 0/0; `ganda repo audit` clean
- [ ] Results and How to validate (dry-run transcript + a real run summary with masked secret)

## Session

- Created: 84930 (2026-09-15)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Manual equivalent (what the command automates):
  `az account show --query tenantId -o tsv`;
  `az ad app create --display-name ... --sign-in-audience AzureADMyOrg --web-redirect-uris ...`;
  `az ad sp create --id <appId>`;
  `az ad app credential reset --id <appId> --display-name dev --years 1 --query password -o tsv`;
  `dotnet user-secrets set "Authentication:Entra:..." ...` in `web-server`.
- Options class: `source/container-apps/web/features/identity/entra-authentication-options-application.cs`
  (`Authentication:Entra` — Enabled, Instance, TenantId, ClientId, ClientSecret, CallbackPath,
  TrustedTenants, AllowBootstrap; `PublicOrigin` arrives with 219-004).
- Existing process-execution examples: `tools/dev-cli/endpoints/db-app-host.cs`, `run-command.cs`.
- Do not store the secret anywhere except `dotnet user-secrets`; no appsettings edits.

## Results

_Pending._

### How to validate

_Pending._
