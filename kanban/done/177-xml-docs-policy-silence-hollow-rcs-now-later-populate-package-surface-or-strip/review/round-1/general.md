# Round 1 — general
**Date:** 2026-09-21
**Scope reviewed:** branch vs origin/master — CS1591 IsPackable gate, packable XML populate, template/app hollow-shell strip, AGENTS.md/.editorconfig policy

## Summary

Path A+B landed cleanly: packable public surface gets real `///` summaries, template/app/tests lose empty `<param>`/`<returns>`/`<typeparam>` shells only, and CS1591 is omitted from the root NoWarn list then re-added in `Directory.Build.targets` `PackableXmlDocs` when `IsPackable != true` (required because child props/csproj flip packability after root props). Effective MSBuild NoWarn confirms packables lack `1591` and non-packables include it; all packable projects plus `web-contracts` build 0/0. Hollow-shell rg is empty; RCS1141/1228 remain `none`; TypedId `EmitBcl` and generated `IAssemblyMarker` carry non-hollow summaries. No defects found.

## Issues
