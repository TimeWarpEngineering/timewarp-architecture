# Dev entra setup: show tenant name and domain, require --tenant when multiple tenants are visible

## Description

Follow-up to 219-005 (`dev entra` command group). On first live use the tester ran
`dev entra setup` while `az` was signed in to the CrunchIt, LLC tenant, then picked an account
from a different tenant in the browser and got Entra's "Selected user account does not exist in
tenant 'CrunchIt, LLC' and cannot access the application ... The account needs to be added as an
external user in the tenant first." The CLI had printed only the tenant GUID and app id, so
nothing on screen said *which organisation* the registration was created in or *which accounts*
would be able to sign in. The tester has more than one Microsoft account and tenant.

Make tenant selection explicit and human-readable in `dev entra setup` and `dev entra status`.

## Requirements

1. **Resolve tenant display name and default domain.** For the signed-in tenant use
   `az rest --method get --url "https://graph.microsoft.com/v1.0/organization?$select=id,displayName,verifiedDomains"`
   (works with the existing `az` login; no extra permissions). Print the tenant line as
   `CrunchIt, LLC (crunchitfs.com) — 30f3971f-...` everywhere a tenant is shown. Tolerate Graph
   failure (print the GUID with "name unavailable"), never fail setup on it.
2. **Enumerate every tenant the CLI can see, including subscription-less ones.** `az account list`
   only lists tenants with a subscription; `az account tenant list` (experimental `account`
   extension; may need `az config set extension.dynamic_install_allow_preview=true` or the
   command may be unavailable) lists tenant ids the identity can access. Combine both, dedupe,
   resolve names where a token for that tenant is available (`az account get-access-token --tenant <id>`
   may require `az login --tenant <id> --allow-no-subscriptions`; do not log in on the user's
   behalf — print the exact `az login` command instead).
3. **`--tenant <id|domain|name>` option on `setup`.** Match by GUID, default domain, or display
   name (case-insensitive). When omitted: if exactly one tenant is visible, use it and print its
   name; if more than one, refuse with a table of the candidates and the `--tenant` hint (the
   dev CLI is non-interactive, so refuse rather than guess). `status` accepts the same option and
   otherwise reports the tenant from user secrets.
4. **Record the choice.** Write `Authentication:Entra:TenantDisplayName` and
   `Authentication:Entra:TenantDomain` to user secrets alongside `TenantId` (informational; the
   options class may ignore them or expose them as optional strings for the boot log — worker
   decides, keep validation permissive). `dev entra status` shows them.
5. **App display name carries the tenant domain** by default:
   `TimeWarp Architecture Dev (crunchitfs.com)`; `--name` still overrides. Find-or-create must
   match on the full default name so existing registrations named without the suffix are not
   duplicated: look for both the suffixed and the bare legacy name and reuse whichever exists.
6. **Print who can sign in.** End of `setup` summary: "Sign in with an @crunchitfs.com account
   (single-tenant app). Other tenants' accounts must be invited as guests first." Also warn when
   `--public-origin` is given but no redirect URI in the final list starts with it.
7. Tests: extend `tests/tools/dev-cli-tests/` (Jaribu, compile-include of pure helpers) for
   tenant matching (`id|domain|name`), the refuse-when-ambiguous decision, default app-name
   suffixing and legacy-name lookup, and the public-origin/redirect warning. No live `az` in tests.
8. Docs: update the "Local Entra setup" section of `source/container-apps/web/projects/web-spa/wwwroot/auth.md`
   with `--tenant`, the multi-tenant refusal, and the guest-user note.

Conventions as in 219-005: Amuru `Shell.Builder("az")` / `CaptureAsync` only, explicit types,
`ITerminal` output, Purpose/Design regions, never print or log the client secret.

## Checklist

- [ ] Tenant name/domain resolution via Graph organization; graceful fallback
- [ ] Tenant enumeration incl. subscription-less tenants; `az login` hint, never auto-login
- [ ] `--tenant` on setup and status; refuse when ambiguous and no `--tenant`
- [ ] `TenantDisplayName` / `TenantDomain` written to user secrets and shown by status
- [ ] Default app name suffixed with tenant domain; legacy bare name reused if present
- [ ] "Who can sign in" summary line; public-origin vs redirect-URI warning
- [ ] Jaribu tests for the pure helpers; `dev build` 0/0; `ganda repo audit` clean
- [ ] auth.md updated
- [ ] Results and How to validate (dry-run transcript showing the tenant table and refusal)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Observed on this machine: `az account list` showed only "Pay-As-You-Go / 30f3971f-... /
  steven.cramer@crunchitfs.com"; `az account tenant list` showed two tenant ids
  (`30f3971f-4719-4f20-9b6f-88916e0b95bd` and `74ca6706-93ef-4a23-b05a-e9ab17b7f86f`); the Graph
  organization call returned `CrunchIt, LLC` / `crunchitfs.com` for the current tenant.
- Files: `tools/dev-cli/endpoints/entra-*.cs`, `tools/dev-cli/services/entra-setup.cs`,
  `entra-cli.cs`, `tests/tools/dev-cli-tests/`, `source/container-apps/web/features/identity/entra-authentication-options-application.cs`.
- Related: 219-005 (dev entra), 219-004 (PublicOrigin), 221 (iss claim fix).

## Results

_Pending._

### How to validate

_Pending._
