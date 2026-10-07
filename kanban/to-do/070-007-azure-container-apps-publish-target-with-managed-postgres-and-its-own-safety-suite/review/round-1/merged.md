# Round 1 — merged findings
**Date:** 2026-10-07
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 5 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:516-581
- Description: On ACA the web hop resolves to `https://web-server.internal.<domain>` while carrying the client's public Host (original-Host transform), so ACA's host-routed internal ingress / TLS name check likely breaks every web route, not only passkeys. The inline comment "TLS terminates at the ingress edge … not on this hop" is false for aca.
- Suggestion: Decide the aca web-route host strategy before relying on the target; at minimum correct the comment and make the skill say web routes are expected to fail until decided.
- Source: general (Issue 1)
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: skills/tw-deploy/SKILL.md:205, 300-304; program.cs aca Design paragraph; task.md Results
- Description: Docs say the Postgres password lives in Key Vault; only the connection string does — the password is also plain container-app secrets (`postgres-db-password`, `postgres-db-uri`) on web-server.
- Suggestion: Correct the docs; optionally pin the shape in the suite.
- Source: general (Issue 2)
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: skills/tw-deploy/SKILL.md:304; program.cs Design
- Description: `AllowAllAzureIps` admits any Azure IP in any tenant, not "Azure services only"; suite does not guard the firewall.
- Suggestion: Reword; add a suite fact pinning the firewall rule set.
- Source: general (Issue 3)
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs:363
- Description: `ExternalIngress` regex requires `external` to be the first ingress key; otherwise External silently defaults to false — false-pass for web/api/grpc.
- Suggestion: Slice the ingress block and read the `external` field; missing field is a failure.
- Source: general (Issue 4)
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: tools/dev-cli/services/aspire-deploy-preflight.cs:65-72; tools/dev-cli/services/aspire-deploy.cs:116-119
- Description: az CLI subscription is injected as an env var whenever the process env is unset, overriding a user-secret `Azure:SubscriptionId` — fallback acts as override.
- Suggestion: Consult AppHost config/user secrets before falling back, or document the override.
- Source: general (Issue 5)
- Disposition notes:

### M6 — Severity: suggestion — Status: open
- File: skills/tw-deploy/SKILL.md:320-333
- Description: Migration recipe never says where the operator gets the Postgres username/password; published `main.bicep` username default is per-publish noise; Key Vault is RBAC with no operator role.
- Suggestion: Add where to find the deployed values.
- Source: general (Issue 6)
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: program.cs:14; tools/dev-cli/endpoints/deploy-command.cs:14-15
- Description: Stale "Postgres is a container resource" Design line; mis-spliced/over-wide Design line in deploy-command.cs.
- Suggestion: Qualify and re-wrap.
- Source: general (Issue 7)
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
