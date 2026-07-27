# Round 2 — general
**Date:** 2026-07-28
**Scope reviewed:** stage 2 commits 156ccb72, f62064da, 6e049ff1 (+ docs e5f5b4a1)

## Summary

Stage 2 cleanly groups api (5), grpc (5), and aspire (2) artifact folders under each family’s `projects/` directory, leaving yarp flat at the family root. Live path surfaces (slnx, ProjectReferences, Dockerfile COPY for grpc, aspire-app-host → servers, web-server aspire-service-defaults + constants Compile Include, aspire.config.json, dev-cli `run-command.cs`, tests) include the `projects/` segment at the correct relative depth; sibling refs within each `projects/` stay one-level. ServiceNames / `AddProject` resource names are unchanged; AGENTS.md Layout documents all four families under `projects/` with yarp flat. No live residual `api/api-*`, `grpc/grpc-*`, or `aspire/aspire-*` source paths outside kanban history (tests-tree paths such as `tests/container-apps/api/api-server-integration-tests` are intentional and unchanged).

## Issues

_(none)_
