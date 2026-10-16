# Round 1 — merged findings
**Date:** 2026-10-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: task.md Checklist item 2
- Description: "SPA endpoint wired end to end" was checked, but the WASM store is not exported.
- Suggestion: Reword the item to match what shipped.
- Source: general
- Disposition notes: Item reworded to "server-side stores exported end to end; WASM store out of scope (see Results)".

### M2 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-server/program.cs:152
- Description: The WASM store (the client half of InteractiveAuto) is not inspected.
- Suggestion: Make it an upstream follow-up.
- Source: general
- Disposition notes: Accepted for this id (decided by the review orchestrator). It needs a WASM-capable
  exporter and a CORS story in timewarp-state that do not exist yet; it is not fixable from the
  template. The limit is documented in the web-server and AppHost Design regions and in Results. The
  follow-up is listed under task Notes for the maintainer.

### M3 — Severity: nit — Status: fixed
- File: task.md How to validate
- Description: The smoke command failed from the repo root and used a partial class name.
- Suggestion: Run it from the test folder with `StateDevToolsModel_Given_`.
- Source: general
- Disposition notes: Command corrected in task.md.

## Duplicates / conflicts

- None.
