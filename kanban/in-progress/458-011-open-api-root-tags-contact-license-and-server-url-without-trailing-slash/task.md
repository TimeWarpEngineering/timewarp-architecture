# OpenAPI root tags contact license and server URL without trailing slash

## Description

The document has no root `tags` (Spectral `operation-tag-defined` ×79), no `info.contact`, and no
`info.license`. `servers[0].url` is `https://arch.timewarp.work/` with a trailing slash (Redocly
`no-server-trailing-slash` error, Spectral warning); clients that concatenate produce `//api/...`.

## Requirements

- A document transformer emits root `tags` from the tags in use, each with a description (for
  example which groups are admin, user, or agent surfaces).
- Set `info.contact.url` and `info.license` from repository metadata.
- Trim the trailing slash in `OpenApiPublicServer`, with a test assertion.
- Document check: every operation tag is declared at the root.

Owner: transformers `open-api-public-server.cs` and document options in `common-server-module.cs`.

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-14 and M-15 (Grok G-012, G-013; Fable F-015, F-017) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.
- Grooming note: pass product tag descriptions from `web-server/program.cs` rather than hardcoding them in shared `foundation-server`; update `open-api-document-servers-tests.cs` (which currently asserts trailing `/`).

## Session

- Created: 2227132 (2026-11-24)
