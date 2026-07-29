# Generator analyzer hardening from task 131 review

## Parent

131

## Description

Implement the generator/analyzer cluster from the task 131 full-repo code review
(findings F-003, F-004, F-005, F-008, F-014). See parent
`disposition.md` and `findings.md`.

## Requirements

- **F-003:** Delete static `RouteRegistry`; per-compilation `.Collect()` + in-batch
  route+verb duplicate reporting (also fixes IDE incremental self-conflict).
- **F-004:** Shared hosted-route discovery as **linked source** into convention-analyzers
  and generators packages (flag-parameterized); fix live
  `[ApiEndpoint]`+`[ClientOnlyContract]` contradiction; dedupe `GetAllNamespaces`.
- **F-005:** Delete `ApiEndpointAttribute.EndpointType` and dead extraction/emission;
  fix `ApiEndpointSourceGenerator.md` that teaches the dead API. Do **not** implement
  the override (YAGNI).
- **F-008:** Fail closed on unrecognized `HttpVerb` (no default to GET); cover
  `ResolveHttpVerbName` fallback; Head/Options already exist on the enum.
- **F-014:** Document TWE/SG IDs in AGENTS.md; consolidate TWE registry (or drop false
  authority claim); wire-or-delete unused TWE001/002/004; dedupe dual SG001.

## Checklist

- [x] F-003 per-compilation route conflict detection
- [x] F-004 shared discovery + ClientOnly×ApiEndpoint fix
- [x] F-005 delete EndpointType + docs
- [x] F-008 fail-closed verbs
- [x] F-014 document/consolidate diagnostic IDs
- [x] Analyzer/generator tests green (sourcegenerator 59, analyzers 99)
- [x] Phase 4b review disposition clean

## Notes

Parent review: `kanban/in-progress/131-complete-repo-code-review-by-kimi-k3/`.
Verification: `review/round-1/claude-verification.md`, `grok-verification.md`.

### Implementation plan (2026-07-29)

See git history / earlier Notes revision for ordered steps. Executed as planned with
review-driven polish on empty-route TWE007 and tests.

## Session

- Created: 2026-07-28 — from task 131 disposition
- Plan: 2026-07-29 — tw-orchestrate-task Phase 2/3
- Implement: 2026-07-29 — Phase 4 (`bcce35a8` + review fixes)
- Review: 2026-07-29 — Phase 4b general, disposition clean

## Results

**What shipped**
- F-003: Deleted static `RouteRegistry`; FastEndpoint uses equatable `EndpointEmitModel` +
  `.Collect()`; TWE003 on all conflict parties; generate none of the group.
- F-004: `source/analyzers/shared/hosted-route-discovery.cs` linked into both packages;
  generators/ingress/coverage consume it; **TWA0020** for ApiEndpoint+ClientOnly.
- F-005: `EndpointType` removed; always emit `BaseFastEndpoint`; docs updated.
- F-008: Fail-closed verbs (allow-list + Head/Options); **TWE007** including missing/empty
  ApiRoute.
- F-014: Central `DiagnosticDescriptors` for TWE/SG; TWE001/004 deleted; TWE002 wired;
  AGENTS.md TWE/SG table.

**Tests:** sourcegenerator-tests **59 passed**; analyzers-tests **99 passed**; web-server
build 0/0 at implement time.

**Review:** `review/` effort 1 general; round-1 disposition **clean** (3 findings fixed
before exit). Paths: `review/review-framework.md`, `review/round-1/{general,merged}.md`,
`review/disposition.md`.
