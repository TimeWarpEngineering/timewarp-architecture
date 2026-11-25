# One problem-details shape on the wire and in OpenAPI including validation 400

## Description

Live `GET /api/Hello` without `name` returns `400` with `content-type: application/json` and a body
with an `errors` map. Every generated operation documents `default` as `application/problem+json`
with `TimeWarpFoundationTypesSharedProblemDetails` (`type,title,status,detail,instance`, no
`errors`). The document also has `MicrosoftAspNetCoreMvcProblemDetails` (used by the Entra
challenge), and bearer 401/403 and payment 402/503 use their own inline problem objects.

## Requirements

- FastEndpoints validation failures are written as `application/problem+json`.
- The one problem schema declares `errors` (map of string arrays) and allows extension members
  (`traceId`).
- Operations with a validator document `400` with that schema.
- Every problem response (default, 400, bearer 401/403, payment 402/503, Entra 400/403) references
  that one component; `MicrosoftAspNetCoreMvcProblemDetails` is gone from the document.
- Test: `GET /api/Hello` without `name` is `application/problem+json` and the published schema lists
  `errors`.

Owner: host config (`UseFastEndpoints` error builder in `source/container-apps/web/projects/web-server/program.cs` and `source/container-apps/api/projects/api-server/program.cs`, or a shared helper in `common-server-module.cs`); transformers `open-api-one-of.cs`, `open-api-bearer-security-server.cs`, `open-api-payment-responses-server.cs`; `challenge-entra-endpoint-server.cs`.

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-05 (Grok G-004, Fable F-009, F-013 part) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.

## Session

- Created: 2227132 (2026-11-24)
