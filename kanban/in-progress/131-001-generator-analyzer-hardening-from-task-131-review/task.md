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

- [ ] F-003 per-compilation route conflict detection
- [ ] F-004 shared discovery + ClientOnly×ApiEndpoint fix
- [ ] F-005 delete EndpointType + docs
- [ ] F-008 fail-closed verbs
- [ ] F-014 document/consolidate diagnostic IDs
- [ ] `dev build` 0/0; analyzer/generator tests green

## Notes

Parent review: `kanban/in-progress/131-complete-repo-code-review-by-kimi-k3/`.
Verification: `review/round-1/claude-verification.md`, `grok-verification.md`.

## Session

- Created: 2026-07-28 — from task 131 disposition
