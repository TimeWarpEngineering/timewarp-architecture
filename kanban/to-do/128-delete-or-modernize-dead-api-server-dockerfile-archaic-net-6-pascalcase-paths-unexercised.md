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

- [ ] Inventory all Dockerfiles under source/container-apps/ and their liveness (workflow,
      Aspire, docs references)
- [ ] Maintainer picks A or B (per-file if liveness differs)
- [ ] Apply; if deleting template content, run `dev template-smoke` both matrices
- [ ] `dev build` 0/0

## Notes

- Origin: 127 round-3 verification finding; predates 127 by years (last substantive touch in
  the pre-kebab-case era, commit 9a940499 and earlier).

## Session

- Created: 2026-07-28 — filed from 127 verification.
