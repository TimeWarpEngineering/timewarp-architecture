# Round 1 — general
**Date:** 2026-11-27
**Scope reviewed:** `9d7e98195` vs `master` (workflow, dev CLI release services, template excludes, `releasing.md`, dev-cli-tests) against the task and `## Goal (implementer restatement)`, plus `## Results` and `### How to validate`.

## Summary

The branch does what the task asks. `ArchitecturePackableProjectService` is the one packable set: MSBuild `IsPackable` and `PackageId` over `source/**/*.csproj` (outside bin/obj) plus the template project, registered only inside a `templateRepository` region. The old hand-kept `PackableProjects` list and `checkVersionStrategy` are gone. A master push or a `mode: full` dispatch on master stages packages; a pull request does not. When ci-group is off, `pack-only` carries a live non-PR `Packages-*` for the same green key or else builds and packs, and the ci green-key marker still uploads only after `ci-group` succeeds. The release job is the six promotion steps (tag assertion, tag pin via `refs/tags/v{Version}^{commit}`, ancestor of `origin/master`, attestation require by default, then check-version, locate-run, download, verify, per-package `--skip-duplicate` push) and then template-publish-smoke. It has `fetch-depth: 0`, `actions: read`, `GH_TOKEN`, and break-glass `confirm: release`. The four new template-only files are excluded from generated apps. `releasing.md` matches that flow. The recorded gates (build, self-install, dev-cli-tests 467/467, template-smoke, check-version listing 12 packages, a local `--phase pack` verify, and repo audit) cover the change. No findings.

## Issues

<!-- None. -->
