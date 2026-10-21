# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general (oracle), general (sub-reviewer a270e187bf5cb516b)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 5 | 0 | 0 |
| nit | 3 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: skills/tw-deploy/references/azure-container-apps-neon.md:143; source/container-apps/aspire/projects/aspire-app-host/aca-neon-compute.cs:10
- Description: Both files claim that ACA or the ingress probes `/alive`. No probe is published (`program.cs:139`: "Compose, Kubernetes and ACA emit none"). The conclusion holds, but the reason is wrong, and this text ships in generated apps.
- Suggestion: Say that no HTTP probe is published. Optionally warn that the anonymous `/api/health` (which runs the `PostgresDbContext` check) wakes Neon, so an uptime monitor should not target it.
- Source: general (oracle), general (sub-reviewer)
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/aspire/projects/aspire-app-host/aca-neon-compute.cs:41-60; tests/container-apps/aspire/aspire-tests/aca-neon-publish-tests.cs:186
- Description: There is no evidence that the culture swap affects Bicep formatting, because Bicep is written after the `finally` restores the culture. The guard regex `0[.,]25` also accepts the comma regression the swap claims to prevent.
- Suggestion: Tighten the regex to `0\.25`. Prove the swap is needed under a comma culture, or remove it and its Design claim.
- Source: general (sub-reviewer), verified by oracle
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/platform/postgres/postgres-db-module-server.cs:149-175
- Description: The override duplicates `PostgresRetryPolicy` and bypasses it. That breaks the policy's claim that tests use the exact production policy. The parsing is untested. `RetryDelaySeconds` is EF's maximum back-off delay, but the code and the guide (line 128) describe it as a plain delay.
- Suggestion: Add a `PostgresRetryPolicy.Configure` overload that takes the bounds and use it in both paths. Test the parsing. Document the knob as a maximum delay.
- Source: general (sub-reviewer), verified by oracle
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: tools/dev-cli/services/aspire-deploy-preflight.cs:190-215; tools/dev-cli/services/aspire-deploy.cs:142
- Description: The aca-neon migrate preflight requires `az login`, the pooled secret, and the pooled-string shape check, but migrate uses only the direct string.
- Suggestion: In the Migrate scope for aca-neon, skip the az probe and resolve and check only the direct secret.
- Source: general (sub-reviewer), verified by oracle
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: tools/dev-cli/endpoints/workflow-command.cs:17; .github/workflows/workflow.yml:226, 293-303
- Description: The new comment "workflow.yml uploads every publish output" is false: aca-neon is published and gated, but not uploaded. The workflow.yml:226 comment still lists three targets.
- Suggestion: Add an aca-neon Bicep upload, or correct both comments.
- Source: general (oracle), general (sub-reviewer)
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: tools/dev-cli/services/neon-connection.cs:42, 156-158
- Description: The check accepts `Channel Binding=Disabled`, which Npgsql rejects; the valid value is `Disable`. `Server=` is not recognized as the host.
- Suggestion: Drop `Disabled`. Accept `server`, or document that the key/value form with `Host=` is required.
- Source: general (sub-reviewer), verified by oracle
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs:129-135, 524; tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs:241
- Description: The "NotForward" assertions cannot fail, because no secrets reach the functions under test. `Publish_Should_KeepAcaScaleAndPostgres` checks only that things are absent.
- Suggestion: Assert the structural separation (secrets stay off `Parameters`, and only `Parameters__*` env entries are written), or rename the tests.
- Source: general (sub-reviewer), verified by oracle
- Disposition notes:

### M8 — Severity: nit — Status: open
- File: skills/tw-deploy/SKILL.md:346; source/container-apps/aspire/projects/aspire-app-host/program.cs:400-418
- Description: SKILL.md scopes the `unused` Entra placeholder to Azure Container Apps, but it applies to every published target. With Entra enabled and no secret set, sign-in fails with AAD `invalid_client`.
- Suggestion: Correct the scope and note the Entra-enabled consequence.
- Source: general (sub-reviewer), verified by oracle
- Disposition notes:

## Duplicates / conflicts

- The `/api/health` note (sub-reviewer issue 2) predates this branch. It was folded into M1 as an optional doc warning.
- Dropped as unverified: sub-reviewer issue 10 (deprovision may need the Neon secrets). The live `dev deprovision --target aca-neon` succeeded.
- Dropped: sub-reviewer issue 11 (version bump). `dev check-version` passes; source 2.0.0-beta.24 is ahead of nuget's beta.23.
