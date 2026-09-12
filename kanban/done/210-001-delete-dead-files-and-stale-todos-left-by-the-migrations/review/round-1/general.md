# Round 1 — general
**Date:** 2026-09-12
**Scope reviewed:** branch task/210-001-delete-dead-files-and-stale-todos-left-by-the-migr vs origin/master (c3b2beb0)

## Summary

Mechanical cleanup of parent-210 findings M11, M16, M31, M35, M37, M38, M39, M40, and M41: dead folders/files removed, stale TODOs cleared or moved into Open Questions, redundant pragmas dropped, and `web-spa` plus monorepo-only `agent-identity-cli-tests` added to `timewarp-architecture.slnx`. Risk is low — deletes and comment hygiene with no design surface change; the `#if (false)` gate on `/tests/tools/` matches the established monorepo-vs-template slnx pattern and aligns with `template.json` exclusions. All nine claimed M-ids are done and ledger statuses/counts match; the only leftover is one product-source comment that still cites the deleted jaribu-tests props path.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs:29-30
- Description: After M11 deleted `tests/foundation/foundation-domain-jaribu-tests/`, this co-located runfile's NoWarn rationale still cites `tests/foundation/foundation-domain-jaribu-tests/Directory.Build.props` as the Jaribu precedent. Comment-only (build/tests unaffected), but it is a dangling product-source path left by the delete.
- Suggestion: Retarget the citation to the live precedent — `tests/Directory.Build.props` and/or `tests/foundation/foundation-domain-tests/foundation-domain-tests.csproj` — or drop the path and keep the generic "tests/ tree Jaribu convention" wording.
- Status: open
