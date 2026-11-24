# Document or exclude browser-navigation routes and liveness in OpenAPI

## Description

- `POST /identity/sign-out` is documented as only `204` with an empty form body; it returns `303`
  (`Location: /Login`) or `400`, and needs the antiforgery form field.
- `GET /identity/sign-out/antiforgery-token` is documented as `204`; live it returns `200
  application/json` `{formFieldName, requestToken}`.
- `GET /api/identity/entra/challenge` documents `204`, has no `mode` query parameter, and omits the
  `302` OIDC redirect.
- `GET /api/health`'s description names `GET /web-server/alive` as liveness; that route is served
  (200 `Healthy`) but is neither an operation nor in `x-timewarp-excluded-routes`, while
  `info.description` says a missing path is an exclusion.

## Requirements

- Decide per route: document the real contract from the endpoint declaration (303 + `Location`, 400,
  antiforgery field; 200 token body; `mode` enum + 302), or exclude the browser-navigation route and
  list it in `x-timewarp-excluded-routes` with a reason. Record the decision in a Design region.
- List liveness in `x-timewarp-excluded-routes` from the `IngressPaths` constants, and generate the
  health description from the same constants.
- Extend the discovery-coverage test so each served non-operation route is listed.

Owner: hand-written identity endpoints; transformer `source/foundation/foundation-server/open-api-discovery-coverage.cs`.

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass
- [ ] Re-fetch the live document after deploy and confirm the finding is gone

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-08 and M-11 (Fable F-006, F-007, F-013; Grok G-009, G-010) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.

## Session

- Created: 2227132 (2026-11-24)
