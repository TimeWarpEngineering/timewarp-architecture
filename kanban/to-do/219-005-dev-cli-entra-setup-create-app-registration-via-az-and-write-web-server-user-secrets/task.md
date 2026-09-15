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

- [x] `entra-group.cs` + `entra-setup-command.cs`, `entra-status-command.cs`, `entra-disable-command.cs`
- [x] Idempotent find-or-create app; redirect URI union; service principal ensure
- [x] Secret minted only on first run / `--new-secret`; never echoed or logged
- [x] User secrets written on web-server project; `--dry-run` prints without executing
- [x] `dev --capabilities` lists the new group; region annotations pass `ganda repo audit`
- [x] Docs: identity guide section "Local Entra setup" pointing at `dev entra setup`
- [x] `dev build` 0/0; `ganda repo audit` clean
- [x] Results and How to validate (dry-run transcript + a real run summary with masked secret)

## Session

- Created: 84930 (2026-09-15)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: grok task-work implement oracle (2026-09-15)

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

`dev entra` command group on `tools/dev-cli`: setup / status / disable. Logged-in `az` plus one command writes Web.Server user secrets for the named `entra` scheme. No portal copy-paste.

**Implemented**
- `dev entra setup [--name] [--public-origin] [--redirect-uri]... [--new-secret] [--dry-run]`
  - Preflight: `az` on PATH; `az account show` (else `az login` hint, exit 1)
  - Find-or-create app by display name (default `TimeWarp Architecture Dev`); `--sign-in-audience AzureADMyOrg`
  - Default redirect URIs `https://localhost:63611/signin-oidc` and `https://localhost:63610/signin-oidc`, plus `{--public-origin}/signin-oidc` and extra `--redirect-uri` values; existing apps get a **union** update, not overwrite
  - Ensure service principal
  - Mint `az ad app credential reset --append --display-name dev-yyyyMMdd --years 1` only when `--new-secret` or `Authentication:Entra:ClientSecret` is absent; password captured in memory only; never printed; never placed on a logged command line
  - `dotnet user-secrets set` on `web-server`: Enabled, TenantId, ClientId, ClientSecret (this run only), TrustedTenants:0, AllowBootstrap, PublicOrigin when given
  - Summary table + next step (`dev run`, Continue with Microsoft 365)
- `dev entra status`: user secrets (secret masked) + app registration redirect URIs
- `dev entra disable`: `Authentication:Entra:Enabled=false` only (no Azure change)
- `--dry-run` prints every `az` / `dotnet user-secrets` invocation and executes none; ClientSecret prints as `********`

**Files**
- `tools/dev-cli/endpoints/entra-group.cs`, `entra-setup-command.cs`, `entra-status-command.cs`, `entra-disable-command.cs`
- `tools/dev-cli/services/entra-setup.cs` (pure union/mask/JSON helpers), `entra-cli.cs` (Amuru `Shell.Builder` runner)
- `tests/tools/dev-cli-tests/` (Jaribu compile-include of the helpers; 11 passed)
- `source/container-apps/web/projects/web-spa/wwwroot/auth.md` — **Local Entra setup**
- `Directory.Packages.props` — dropped unused `Microsoft.Identity.Web` / `Microsoft.Authentication.WebAssembly.Msal` CPM pins (219-002 leftover; blocking `cpm-consistency`)

**Decisions / deviations**
- Process execution is Amuru `CaptureAsync` only; stdout of credential reset is never written to the terminal
- Status looks up the app by user-secrets `ClientId` first, then default display name
- `PublicOrigin` is written when `--public-origin` is given even if 219-004 has not merged the option yet
- `--dry-run` prints both create and merge/SP-create branches with `<appId>` placeholders because it does not call Azure

**Test outcomes**
- `dotnet run tools/dev-cli/dev.cs -- build`: 0 Warning(s), 0 Error(s)
- `cd tests/tools/dev-cli-tests && dotnet test -c Release`: 11 passed, 0 failed
- `ganda repo audit`: blocking checks pass (region-annotations, kebab-path-names, cpm-consistency, dev-cli-capabilities). Advisory warnings only: memsearch-scaffold, vscode-window-icon (pre-existing)
- `./bin/dev --capabilities` lists `entra setup`, `entra status`, `entra disable` (re-self-install after this change)
- Live `az` (tenant `30f3971f-…`, user `steven.cramer@crunchitfs.com`): created app `f6d55605-aed5-4b5e-8b54-6260de4b323a`, minted `dev-20260915` (masked), second setup reused the app and left the secret unchanged, disable set Enabled=false, third setup re-enabled without minting

### How to validate

**Smoke**

```bash
dotnet run tools/dev-cli/dev.cs -- --capabilities
# or after self-install: ./bin/dev --capabilities

dotnet run tools/dev-cli/dev.cs -- entra --help
dotnet run tools/dev-cli/dev.cs -- entra setup --help
dotnet run tools/dev-cli/dev.cs -- entra setup --dry-run --public-origin https://arch.timewarp.work
```

**Expect**
- Capabilities JSON includes patterns `entra setup`, `entra status`, `entra disable`
- `--dry-run` prints `az account show`, `az ad app list/create/show/update`, `az ad sp show/create`, `az ad app credential reset --append --display-name dev-yyyyMMdd --query password`, and `dotnet user-secrets set` lines
- Dry-run ClientSecret line is `********`, never a real password; exit 0; "Dry-run: no Azure or user-secrets changes."
- Redirect URI list includes `https://localhost:63611/signin-oidc`, `https://localhost:63610/signin-oidc`, and `https://arch.timewarp.work/signin-oidc`

**Live setup (needs `az login`)**

```bash
az account show --query tenantId -o tsv
dotnet run tools/dev-cli/dev.cs -- entra setup
dotnet run tools/dev-cli/dev.cs -- entra status
dotnet run tools/dev-cli/dev.cs -- entra setup          # idempotent: reuse app, Client secret (unchanged)
dotnet run tools/dev-cli/dev.cs -- entra disable
dotnet run tools/dev-cli/dev.cs -- entra status         # Enabled = false; Azure app unchanged
```

**Expect (live run on this machine)**
- First setup: Created app `f6d55605-aed5-4b5e-8b54-6260de4b323a` (TimeWarp Architecture Dev); Client secret `******** (minted this run)`
- Status: `Authentication:Entra:ClientSecret` = `********`; redirect URIs `https://localhost:63610/signin-oidc` and `https://localhost:63611/signin-oidc`
- Second setup: Reusing app registration; Client secret `(unchanged)`
- Disable: `Authentication:Entra:Enabled=false`; status Enabled `false`; app still present
- Next step line: `dev run`, browse, click "Continue with Microsoft 365"

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

cd tests/tools/dev-cli-tests && dotnet test -c Release
# expect: 11 passed, 0 failed

ganda repo audit
# expect: blocking checks pass (region-annotations, kebab-path-names, cpm-consistency, dev-cli-capabilities)
```

**Depends on:** Azure CLI on PATH and `az login` for live setup/status Azure lookup. `--dry-run` and the Jaribu helper tests do not need Azure.

**Not in scope:** clicking Continue with Microsoft 365 in a browser (needs `dev run`); 219-004 `PublicOrigin` binding (the key is written and inert until that option exists). Re-self-install (`dotnet run tools/dev-cli/dev.cs -- self-install`) before expecting `./bin/dev entra`.
