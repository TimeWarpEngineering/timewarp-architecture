# dev entra setup: mint a new client secret automatically when the tenant or app registration changes

## Description

Gap seen live on 2026-09-16: `dev entra setup` mints a client secret only on first write or with
`--new-secret` (`EntraSetup.TryDecideMintClientSecret`: `mint = !hasExistingClientSecret`).
When the tester switched from the CrunchIt tenant to the TimeWarp tenant, setup created a new
app registration and wrote the new `TenantId` / `ClientId`, but without `--new-secret` it would
have kept the CrunchIt app's secret in user secrets. Sign-in then fails at code redemption with a
confusing Entra error, not at setup. The cockpit had to tell the tester to pass `--new-secret`
by hand.

A secret belongs to one app registration. If the `ClientId` (or `TenantId`) that setup is about
to write differs from what user secrets hold, the stored secret is for a different app and must
be replaced.

## Requirements

- In `EntraSetup.TryDecideMintClientSecret` (pure helper, tested), add the inputs
  `existingClientId`, `existingTenantId`, `targetClientId`, `targetTenantId`. Decision:
  - `--new-secret` → mint.
  - no existing secret → mint.
  - existing secret **and** (`existingClientId != targetClientId` or `existingTenantId !=
    targetTenantId`, GUID-equal, case-insensitive) → mint, and print one line: "App registration
    changed (was `{old}` in `{oldTenantName|guid}`); minting a new client secret." Still never
    print the secret.
  - existing secret and same app/tenant → keep (unchanged behaviour).
- `entra-setup-command.cs` `MaybeMintSecretAsync`: pass the four ids; the summary table's
  "Client secret" row says `(minted: app registration changed)` in that case.
- `--dry-run` prints the decision and the (masked) `az ad app credential reset` invocation.
- `dev entra status`: warn when the stored `ClientId` does not match the app registration found
  by name/tenant (already looked up), pointing at `dev entra setup` to re-mint.
- Tests in `tests/tools/dev-cli-tests/`: decision matrix (flag / no secret / changed app /
  changed tenant / unchanged) for the pure helper; dry-run transcript assertion if cheap.
- `auth.md` "Local Entra setup": note that switching tenants re-mints automatically and
  `--new-secret` is only for rotating a secret on the same app.

## Checklist

- [x] `TryDecideMintClientSecret` decision extended + tests
- [x] Setup passes ids, prints the reason, summary row updated; dry-run shows the decision
- [x] Status warns on ClientId mismatch
- [x] auth.md updated
- [x] `dev build` 0/0; `ganda repo audit` clean; `cd tests/tools/dev-cli-tests && dotnet test -c Release` green
- [x] Results and How to validate (dry-run transcript for the changed-app case)
- [x] Implementation review (effort 1, general); disposition clean

## Session

- Created: cockpit (2026-09-17)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok 4.6 (2026-09-17)
- Review oracle: Grok 4.6 (2026-09-17); round-1 general: Grok 4.5 sub-agent

## Notes

- Files: `tools/dev-cli/services/entra-setup.cs` (`HasClientSecret` ~L163,
  `TryDecideMintClientSecret` ~L169-187), `tools/dev-cli/endpoints/entra-setup-command.cs`
  (`MaybeMintSecretAsync` ~L438), `entra-status-command.cs`,
  `source/container-apps/web/projects/web-spa/wwwroot/auth.md`.
- Prior: 219-005 (dev entra), 223 (tenant name / `--tenant`), 227 (single tenant).
- Secrets: never printed, never on a logged command line — keep the 219-005 rules.
- Review kitchen (effort 1, general, 1 round, disposition clean): `review/review-framework.md`,
  `review/round-1/merged.md`, `review/disposition.md`.

## Results

`dev entra setup` re-mints the client secret when stored `ClientId` or `TenantId` differs from
the app it is about to write (GUID-equal, case-insensitive). `--new-secret` remains a same-app
rotation flag. The secret is never printed.

**Files**
- `tools/dev-cli/services/entra-setup.cs` — `TryDecideMintClientSecret` takes the four ids and
  `out appRegistrationChanged`; message/summary/dry-run helpers
- `tools/dev-cli/endpoints/entra-setup-command.cs` — passes ids; prints the changed-app line;
  summary row `(minted: app registration changed)`; dry-run prints the decision and only then
  the masked `az ad app credential reset` invocation. App list is read-only under `--dry-run`
  so the target `AppId` is real
- `tools/dev-cli/services/entra-cli.cs` — `user-secrets list` always executes (read-only)
- `tools/dev-cli/endpoints/entra-status-command.cs` — warns when stored `ClientId` differs from
  the app found by name/tenant; points at `dev entra setup`
- `tests/tools/dev-cli-tests/entra-setup-tests.cs` — decision matrix + dry-run transcript strings
- `source/container-apps/web/projects/web-spa/wwwroot/auth.md` — switching tenants re-mints;
  `--new-secret` is same-app rotation only

**Decisions**
- `--new-secret` still short-circuits listing and does not report `appRegistrationChanged`
- Same app/tenant (including GUID brace/casing variants) keeps the stored secret
- Dry-run of an unchanged app prints `dry-run: keeping the existing client secret.` and does
  **not** print `credential reset`

**Tests / gates**
- `dotnet run tools/dev-cli/dev.cs -- build` — 0 Warning(s) 0 Error(s)
- `cd tests/tools/dev-cli-tests && dotnet test -c Release` — 60 passed, 0 failed
- `ganda repo audit` — blocking checks pass (2 advisory warnings: memsearch-scaffold, vscode-window-icon)

**Review (effort 1, general only; 1 round)**

- Roster: `general` (Grok 4.5 sub-agent). Round 1 raised no findings. Orchestrator re-verified the decision helper, setup/status wiring, dry-run keep-vs-mint paths, fail-closed list, secret non-disclosure, `auth.md`, and `dotnet test` 60/60.
- Final counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0 / 0 / 0; nit 0 / 0 / 0.
- **Disposition:** clean (0 open).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

**Automated**

```bash
cd tests/tools/dev-cli-tests && dotnet test -c Release
# expect: all passed (includes flag / no secret / changed app / changed tenant / unchanged,
#         GUID casing, and the changed-app dry-run transcript strings)
```

**Smoke (same app — keep)**

```bash
dotnet run tools/dev-cli/dev.cs -- self-install   # if ./bin/dev is missing
./bin/dev entra setup --tenant timewarp.enterprises --dry-run
```

**Expect**
- `dry-run: keeping the existing client secret.`
- no `az ad app credential reset` line
- summary **Client secret** = `(unchanged)`
- no plaintext secret on any line

**Smoke (changed app — mint decision; restore ClientId afterwards)**

```bash
PROJECT=source/container-apps/web/projects/web-server/web-server.csproj
REAL=$(dotnet user-secrets list --project "$PROJECT" | awk -F ' = ' '/Authentication:Entra:ClientId /{print $2}')
dotnet user-secrets set "Authentication:Entra:ClientId" "11111111-1111-1111-1111-111111111111" --project "$PROJECT"
./bin/dev entra setup --tenant timewarp.enterprises --dry-run
dotnet user-secrets set "Authentication:Entra:ClientId" "$REAL" --project "$PROJECT"
```

**Expect** (exit 0; secret never in plaintext):

```text
App registration changed (was `11111111-1111-1111-1111-111111111111` in `TimeWarp Enterprises LLC`); minting a new client secret.
dry-run: minting a client secret (app registration changed).
dry-run: az ad app credential reset --id <real-app-id> --append --display-name dev-yyyyMMdd --years 1 --query password -o tsv
******** (minted: app registration changed)
Dry-run: no Azure or user-secrets changes.
```

**Status mismatch**

```bash
./bin/dev entra status --tenant timewarp.enterprises
# with stored ClientId ≠ the app found by name:
```

**Expect**

```text
Warning: stored ClientId … does not match the app registration … found by name in this tenant. Run `dev entra setup` to re-mint.
```

**Depends on:** Azure CLI on PATH and `az login` for live `--dry-run` / status. Helper tests do not need Azure.

**Not in scope:** live `az ad app credential reset` (that mints a real Azure password); browser sign-in after a tenant switch.
