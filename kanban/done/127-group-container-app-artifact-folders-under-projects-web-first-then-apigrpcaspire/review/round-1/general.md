# Round 1 — general
**Date:** 2026-07-27
**Scope reviewed:** commits 267b4523 + ad19d511 (web projects/ regroup)

## Summary

Stage 1 cleanly groups the six web artifact folders under `source/container-apps/web/projects/` while leaving `features/`, `platform/`, and `msbuild/` at the family root. Live path references (slnx, ProjectReferences, template.json spa excludes under `(!grpc)`/`(!api)`, scripts, gitignore, tests, yarp → web-contracts, aspire → web-server/web-contracts) include the `projects/` segment at the correct relative depth; sibling refs within `projects/` stay one-level (`..\web-contracts\`). ServiceNames / `AddProject` resource names are unchanged (TWA0007), AGENTS.md and `tw-feature-placement` document `projects/`, and yarp remains a single-project family (not regrouped). Residual `web/web-*` path strings outside historical kanban/done (and skill analysis scratch) are only intentional tests-tree paths (`tests/container-apps/web/web-*-tests`), not source artifact locations.

## Issues

_(none)_
