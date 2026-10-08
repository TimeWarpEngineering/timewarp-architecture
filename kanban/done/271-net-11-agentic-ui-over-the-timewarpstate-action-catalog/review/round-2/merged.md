# Round 2 — merged findings
**Date:** 2026-10-09
**Sources:** general (general + security re-review of fix commit 3330690db)

## Prior findings

M1–M12 and M14–M17 were verified fixed. The M13 wontfix stands. Nothing was reopened. Details are in `general.md`.

## Counts (new findings this round)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### N1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs
- Description: After approval, the fresh permission check awaits after the navigation listener has been removed, and it reuses the path captured at call time. A navigation during that await could still execute the action on the old page.
- Source: general
- Disposition notes: Fixed in aa75fadff. `IsOnPath(path)` is checked again right before `Execute`.

### N2 — Severity: nit — Status: fixed
- File: web-mcp-dispatcher.cs
- Description: A navigation that ends on the same path (for example a query-only change) cancelled the call and reported `approved:false`, as if the person had rejected it.
- Source: general
- Disposition notes: Fixed in aa75fadff. The wait now records that navigation happened and returns `PageChangedError`. New test: `WebMcp_Same_Path_Navigation_Reports_Page_Changed_Not_Rejected`.

### N3 — Severity: nit — Status: fixed
- File: skills/tw-blazor/SKILL.md:153
- Description: The skill implied every record carries a Version token. Only site settings has one.
- Source: general
- Disposition notes: Fixed in aa75fadff (wording).
