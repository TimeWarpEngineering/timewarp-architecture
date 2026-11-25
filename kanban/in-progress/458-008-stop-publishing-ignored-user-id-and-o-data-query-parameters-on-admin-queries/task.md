# Stop publishing ignored userId and OData query parameters on admin queries

## Description

`GET /api/Roles` publishes required query `userId` (from `[AuthApiRequest]`), optional `top`, `skip`,
`filter`, `orderBy` (from `[OpenDataQueryParameters]`), and required `returnTotalCount`. The handler
orders roles by name and reads none of them. The same required `userId` appears on `GET` and
`DELETE /api/Roles/{roleId}`, `GET /api/admin/principals`, and `GET /api/identity/credentials`. A
generated client cannot call these without inventing a Guid, and `top` would not page.

## Requirements

- Sweep every contract with `[AuthApiRequest]` or `[OpenDataQueryParameters]`.
- Where the handler does not read a member, stop publishing it (remove the attribute or the member).
  Where paging is the intended contract, implement it with a test that `top` changes the row count.
- Decide whether `IAuthApiRequest.UserId` belongs on the wire at all now that authorization uses the
  session or bearer principal; record the decision in that interface's Design region.
- Add a check (test or analyzer) that a required query parameter is read by its handler, if that can
  be done without inference.

Owner: contracts (`get-roles-contracts.cs` and siblings); `source/foundation/foundation-contracts/base/i-auth-api-request.cs`.

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-09 (Grok G-008) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.
- Grooming note: removing `IAuthApiRequest.UserId` from the wire affects `open-api-delete-request-body-tests.cs` (which asserts `userId` on `DELETE`/`GET /api/Roles/{roleId}`), up to 16 `[AuthApiRequest]` contracts, SPA `role-state` callers, `EndpointAuthPostureAnalyzer` (`TWA0014`), and `skills/tw-web-api-contracts/SKILL.md`.

## Session

- Created: 2227132 (2026-11-24)
