# One route grammar for API routes enforced by an analyzer

## Description

Route segments split three ways: PascalCase `/api/Feedback`, `/api/Roles`, `/api/Users/Current/Profile`,
`/api/Hello`; kebab `/api/agent-links`, `/api/admin/feedback`, `/api/identity/...`; and
`/Analytics/TrackEvent` and `/identity/sign-out` outside `/api`. User feedback is `/api/Feedback/...`
while admin feedback is `/api/admin/feedback/...`. `route-grammar.md` covers parameter tokens only, so
nothing enforces a style.

## Requirements

- Write one grammar in `skills/tw-web-api-contracts/references/route-grammar.md` (kebab segments
  under `/api/`).
- Enforce it with an analyzer on `[ApiRoute]` strings (new TWA id; record in
  `AnalyzerReleases.Unshipped.md`).
- Rename the non-conforming routes; regenerate the ingress golden (`dev regenerate-generated`) and
  update SPA callers. This breaks external callers of the demo routes, which the template accepts.
- Browser sign-out routes coordinate with task 458-007.

Owner: contracts (`[ApiRoute]`), skill `tw-web-api-contracts`, convention analyzers.

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass
- [ ] Re-fetch the live document after deploy and confirm the finding is gone

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-13 (Grok G-010, Fable F-014) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.

## Session

- Created: 2227132 (2026-11-24)
