# Review framework — task 126-005

**Date:** 2026-07-26
**Host task:** kanban/in-progress/126-005-fix-scalar-openapi-pipeline-delete-dead-featureannotations-drop-feature-annotations-registry-entry/
**Diff scope:** commit `429d5d65` on `dev` — feat(server): wire FastEndpoints OpenAPI + fix Scalar feature tags (25 files)
**Plan / brief:** Wire FastEndpoints.OpenApi document pipeline for Scalar; fix generator leaf feature tags + Description.WithTags; delete 7 dead FeatureAnnotations; drop feature-annotations registry entry; docs/tests update. x-tagGroups deferred.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator Phase 4b 2026-07-26

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Key paths in scope

- `source/foundation/foundation-server/common-server-module.cs`
- `source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs`
- `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs`
- `source/container-apps/web/web-server/program.cs`, `api/api-server/program.cs`
- `Directory.Packages.props`, foundation-server.csproj
- Feature grammar JSON + generated artifacts; AGENTS.md / tw-feature-placement skill
- Analyzer + sourcegenerator tests
