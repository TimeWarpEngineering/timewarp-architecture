# Delete or modernize dead api-server Dockerfile (archaic .NET 6 PascalCase paths, unexercised)

## Description

Found during task 127's independent verification (2026-07-28):
`source/container-apps/api/projects/api-server/Dockerfile` is dead debris.

Evidence:
- Content is archaic: `dotnet_version=6.0`, PascalCase `Source/ContainerApps/Api/Api.Contracts/...`
  COPY paths (pre-kebab-case rename), and a `Common.*` layer naming scheme that exists nowhere
  else in the repo. Its internal paths have been broken since long before 127; the 127 move only
  renamed its parent folder.
- Nothing exercises it: zero `Dockerfile` references in `.github/workflows/*.yml`; zero
  `WithDockerfile` in source/; the Aspire AppHost uses `.AddProject<Projects.api_server>`
  directly. It cannot have built successfully in years.

Options (maintainer picks at execution):
- **A — delete** (lean): dead, unexercised, and misleading template content; container images
  for generated apps are an Aspire-publish concern, not a hand-written-Dockerfile concern.
- **B — modernize**: only if a real container-build path is wanted for api-server outside
  Aspire; then inventory ALL container-app Dockerfiles for the same rot and fix consistently
  (check whether grpc/web have live or equally dead ones) rather than fixing one.

## Checklist

- [x] Inventory all Dockerfiles under source/container-apps/ and their liveness (workflow,
      Aspire, docs references)
- [x] Maintainer picks A or B (per-file if liveness differs)
- [x] Apply; if deleting template content, run `dev template-smoke` both matrices
- [x] `dev build` 0/0

## Notes

- Origin: 127 round-3 verification finding; predates 127 by years (last substantive touch in
  the pre-kebab-case era, commit 9a940499 and earlier).

## Session

- Created: 2026-07-28 — filed from 127 verification.

## Results

**Landed** (commit `2a0c8bd7`): deleted `source/container-apps/api/projects/api-server/Dockerfile`
(41 lines — .NET 6, PascalCase pre-kebab paths, extinct `Common.*` layer scheme; unbuildable for
years, zero references from workflows/Aspire/template.json/slnx/docs).

**Inventory finding that shaped the ruling:** exactly two Dockerfiles existed, and they are NOT
the same case. `grpc-server/Dockerfile` is actively maintained — .NET 10, current kebab-case
`projects/` paths (kept current through task 127's move). It STAYS as the living containerization
pattern; when task 118 gives the api family real content, api clones the grpc pattern rather than
resurrecting the corpse (recorded on the maintainer's delete ruling, option A, 2026-07-28).

**Verification:** `dev build` 0/0; `dev template-smoke` SUCCEEDED both matrices (Dockerfile was
template content; no references existed to update). Review: orchestrator-inline, proportionate to
an evidence-backed deletion — the liveness sweep (zero references) IS the verification; no
findings.

## Session

- Executed: 2026-07-28 — inline by orchestrator (Claude Fable) per maintainer ruling; evidence
  from 127 round-3 verification.
