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

- [ ] `TryDecideMintClientSecret` decision extended + tests
- [ ] Setup passes ids, prints the reason, summary row updated; dry-run shows the decision
- [ ] Status warns on ClientId mismatch
- [ ] auth.md updated
- [ ] `dev build` 0/0; `ganda repo audit` clean; `cd tests/tools/dev-cli-tests && dotnet test -c Release` green
- [ ] Results and How to validate (dry-run transcript for the changed-app case)

## Session

- Created: cockpit (2026-09-17)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Files: `tools/dev-cli/services/entra-setup.cs` (`HasClientSecret` ~L163,
  `TryDecideMintClientSecret` ~L169-187), `tools/dev-cli/endpoints/entra-setup-command.cs`
  (`MaybeMintSecretAsync` ~L438), `entra-status-command.cs`,
  `source/container-apps/web/projects/web-spa/wwwroot/auth.md`.
- Prior: 219-005 (dev entra), 223 (tenant name / `--tenant`), 227 (single tenant).
- Secrets: never printed, never on a logged command line — keep the 219-005 rules.

## Results

_Pending._

### How to validate

_Pending._
