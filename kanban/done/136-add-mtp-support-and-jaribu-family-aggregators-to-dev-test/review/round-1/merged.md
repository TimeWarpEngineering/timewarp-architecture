# Round 1 — merged findings
**Date:** 2026-07-31
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: `skills/tw-feature-placement/SKILL.md` / `template-smoke-harness.cs` JaribuFamilyAggregators
- Description: Skill documented ProjectReference maintenance but not smoke expected-count bumps when test method totals change.
- Suggestion: Document `JaribuFamilyAggregators` expected counts as required maintenance.
- Source: general
- Disposition notes: Added bullet under co-located runfile preamble (2026-07-31 fix pass).

### M2 — Severity: suggestion — Status: fixed
- File: `tools/dev-cli/services/template-smoke-harness.cs` TryParseMtpSummary
- Description: Parser only accepted multi-line `total:` / `succeeded:` lines; compact `Test summary: total: N, … succeeded: N` would fail-closed.
- Suggestion: Also accept compact form.
- Source: general
- Disposition notes: Regex accepts both forms; last match wins (2026-07-31 fix pass). Multi-line form still green in full template-smoke.

### M3 — Severity: nit — Status: fixed
- File: aggregator `global.json` vs root `global.json`
- Description: SDK pin drift risk with no AGENTS reminder.
- Suggestion: Note that aggregator global.json SDK pins must mirror root on bumps.
- Source: general
- Disposition notes: AGENTS.md Stack/tests blurb updated (2026-07-31 fix pass).

## Duplicates / conflicts

- None.
