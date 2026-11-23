# Round 1 — merged findings
**Date:** 2026-11-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.agent-page-facts.cs:43
- Description: The no-snapshot agent facts are built in two steps (null, then `JsonValue.Create`),
  although `JsonNode` converts `bool?` / `long?` implicitly. With no snapshot, `entraAcceptedTenants`
  is `[]`, not the `null` the Design line describes.
- Suggestion: One initializer with `settings?.X`. Null tenants with no snapshot, or a Design line that
  says the list is empty.
- Source: general
- Disposition notes: wontfix (review oracle). The behavior meets the acceptance line ("A missing
  snapshot does not report version 0, Soft, Restricted, Akili, or Allow"). `hasSnapshot: false` sits
  beside the empty list, so the agent cannot read it as a saved empty policy. The test asserts the
  current shape. A fix pass and a second round cost more than a cosmetic change is worth. The change
  can ride along the next time this file is edited.

## Duplicates / conflicts

- None (single reviewer).
