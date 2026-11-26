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

## Goal (implementer restatement)

The published web OpenAPI document must declare its tags, say who owns it and under what
license, and give a server URL a client can append a path to.

Acceptance criteria:

- The document has a root `tags` array with one entry for every tag an operation uses. A shared
  foundation transformer builds it after every other transformer, so host-added operations count.
- Each root tag on the web-server document has a description. The descriptions are passed from
  web-server (`program.cs`), not hardcoded in foundation-server. A tag in use with no description
  fails the document (500 with the tag name and the fix), so a new feature slice cannot ship an
  undescribed tag.
- A document check registered last fails when an operation tag is not declared at the root.
- `info.contact.url` and `info.license` come from repository metadata (`PackageProjectUrl` /
  `RepositoryUrl` and `PackageLicenseExpression` in MSBuild, baked into the host assembly), not
  from hand-written strings.
- `OpenApiPublicServer` trims the trailing slash, so `servers[0].url` is
  `https://arch.timewarp.work`. `open-api-document-servers-tests.cs` asserts no trailing slash.
- Tests cover root tags and descriptions on the real web-server endpoint set, contact and
  license equal to the assembly metadata, and the server URL.

## Checklist

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Review

- Round 1: Done

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
- 2026-11-26 implement (ganda task work): shared root-tags transformer and check, document info
  (contact, license, tag descriptions), trailing-slash trim, web-server tag map, tests, skill row.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session df953187-d6b4-4f20-8f86-e8851d5da501, max-turns 500, Done — 2026-11-26T02:57:52Z
- 2026-11-26 review round 1 (ganda task work): general only, effort 3. Disposition clean (0 open).
- Review oracle: grok session 01a215c9-dbc2-75f2-91ca-3573a507a588, max-turns 200 (2026-11-26)
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a215c9-dbc2-75f2-91ca-3573a507a588, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-26T03:12:57Z

## Results

What changed:

- `source/foundation/foundation-server/open-api-root-tags.cs` (new): `OpenApiRootTags` runs after
  the host `configureOpenApi` callback and writes root `tags` in ordinal order, one per tag an
  operation uses, keeping any root tag already present and filling its description from the host
  map. Its document check, registered last with the other checks, throws (500 on
  `/openapi/v1.json`) when an operation tag is not declared at the root, and, when the host passed
  descriptions, when a root tag has none.
- `source/foundation/foundation-server/open-api-document-info.cs` (new, public):
  `OpenApiDocumentInfo` carries contact URL, SPDX license, and tag descriptions.
  `FromAssembly` reads `AssemblyMetadata` `OpenApiContactUrl` and `OpenApiLicense` and throws with
  the fix when either is missing. The transformer sets `info.contact.url` and
  `info.license` (`name` and `identifier` = the SPDX expression).
- `msbuild/open-api-document-metadata.props` (new): writes those two metadata keys from
  `PackageProjectUrl` (else `RepositoryUrl`) and `PackageLicenseExpression` in
  `source/Directory.Build.props`. Imported by `web-server.csproj` and `api-server.csproj`.
- `source/foundation/foundation-server/open-api-public-server.cs`: every server URL (forwarded or
  framework) is written without a trailing slash; `https://arch.timewarp.work/` becomes
  `https://arch.timewarp.work`.
- `source/foundation/foundation-server/common-server-module.cs`: new optional `documentInfo`
  parameter on `AddOpenApi`; registers the info transformer, root tags after the host callback,
  and the root-tags check last.
- `source/container-apps/web/projects/web-server/web-server-open-api-tags.cs` (new):
  `WebServerOpenApiTags.Descriptions` (14 tags; `Feedback` under `#if postgres`) say who each group
  is for (anonymous, signed-in user, agent bearer, admin). `program.cs` passes
  `WebServerOpenApiTags.DocumentInfo()`. api-server passes contact and license with no tag map.
- Tests: `open-api-root-tags-tests.cs` (new) and `open-api-document-servers-tests.cs` (now expects
  no trailing slash).
- Skill `tw-web-api-contracts`: one pitfall row (a new slice's tag needs a description); one
  wrapped line joined to stay under the 500-line skill lint cap.

Not done here: post-deploy re-fetch of `https://arch.timewarp.work/openapi/v1.json` (needs a
deploy). api-server's document test needs the Aspire testing AppHost and was not run locally; CI
runs it. The generated api-server assembly was checked for both metadata keys.

Gates:

- `./bin/dev build` — passed, 0 warnings, 0 errors.
- `./bin/dev test --project web-server-integration-tests --filter-class OpenApiRootTags_ --no-build`
  (`open-api-root-tags-tests.cs`) — 5/5 passed.
- `./bin/dev test --project web-server-integration-tests --filter-class OpenApiDocument_.Servers_Given_ --no-build`
  (`open-api-document-servers-tests.cs`) — 4/4 passed.
- `./bin/dev test --project web-server-integration-tests --filter-class OpenApi --no-build`
  (every OpenAPI test class in the project, since the shared module changed) — 107/107 passed.
- Test files are project classes, not standalone runfiles (no `#if !JARIBU_MULTI` guard), so
  they run through `./bin/dev test --project … --filter-class …`.
- `./bin/dev lint-skills` — passed.
- `ganda repo audit` — passed with one advisory warning (`vscode-window-icon`: the gitignored,
  local `.vscode/settings.json` was missing). `ganda repo audit --fix` wrote it (not tracked), and
  the rerun passed all checks.

### How to validate

Smoke:

```bash
./bin/dev build
./bin/dev test --project web-server-integration-tests --filter-class OpenApiRootTags_
./bin/dev test --project web-server-integration-tests --filter-class OpenApiDocument_.Servers_Given_
```

Expect: build 0 warnings; 5/5 and 4/4 pass. In the web-server document, root `tags` equals the
set of operation tags and each has a description; `info.contact.url` is the repository URL and
`info.license` is `{"name":"Unlicense","identifier":"Unlicense"}`; `servers[0].url` has no
trailing slash. After deploy, `curl -s https://arch.timewarp.work/openapi/v1.json | jq '.tags | length, .info.contact, .info.license, .servers'`
shows the same, and Spectral `operation-tag-defined` and Redocly `no-server-trailing-slash` no
longer fire.

### Review

Round 1, effort 3, roster general. Counts: bug 0, suggestion 0, nit 0 (open 0, fixed 0, wontfix 0). Disposition: **clean**. No wontfix and no escalation. The implementer restatement matches the original requirements and the grooming note (descriptions from web-server, servers test updated for the trimmed URL). No misread.

- `review/review-framework.md`
- `review/round-1/general.md`
- `review/round-1/merged.md`
- `review/disposition.md`
