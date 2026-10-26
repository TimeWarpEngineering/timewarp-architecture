# Round 1 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: skills/tw-deploy/SKILL.md:288
- Description: The `### SigNoz link on the local dashboard` subsection is spliced into the middle of the "Rules for new code:" bullet list. The final `yarp` rule bullet is cut off and now sits under the SigNoz heading after a code fence.
- Suggestion: Move the SigNoz subsection after the complete Rules list, before `## Secrets and parameters`.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/container-apps/aspire/aspire-tests/compose-publish-tests.cs:144 (also kubernetes-publish-tests.cs:168, aca-publish-tests.cs:148, aca-neon-publish-tests.cs:131)
- Description: The publish-output `signoz` absence checks run against fixtures that never set `Observability:SigNoz:Url`. They pass trivially, and they would still pass if the run-mode gate were removed. The task's "publish output unchanged" check for the configured case is not covered at the output level.
- Suggestion: Add `--Observability:SigNoz:Url=https://observability.example` to the publish fixtures' args, or drop the vacuous output checks.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:107
- Description: The SigNoz Design paragraph splits the `Ingress:PublicUrl` paragraph from its `Authentication:Entra:PublicOrigin` continuation.
- Suggestion: Move the SigNoz paragraph after the PublicOrigin sentences, before "Public-host forwarding".
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
