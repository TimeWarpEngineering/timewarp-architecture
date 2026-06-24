# 011_Create-TimeWarp-Automation-Library.md

## Description

Create TimeWarp.Automation class library and its test project

## Requirements

```pwsh
# Create library
dotnet new classlib -n TimeWarp.Automation -o Source/Libraries/TimeWarp.Automation

# Create test project following:
# https://github.com/TimeWarpEngineering/timewarp-fixie
dotnet new classlib -n TimeWarp.Automation.Tests -o Tests/Libraries/TimeWarp.Automation.Tests
dotnet add Tests/Libraries/TimeWarp.Automation.Tests/TimeWarp.Automation.Tests.csproj package Fixie
dotnet add Tests/Libraries/TimeWarp.Automation.Tests/TimeWarp.Automation.Tests.csproj package Shouldly

## CANCELLED (2026-06-24) — library dropped

TimeWarp.Automation was RPA-style "robot automation" (launch a GUI app via `ProcessStartInfo`,
wait for its window handle, control window style). It was **unused** (nothing in the repo
referenced it but its own test), only ~218 production LOC across 2 features, and its approach is
superseded by where we'd take this now (AI-driven desktop automation) — and it tripped the repo's
`ProcessStartInfo` ban (→ TimeWarp.Amuru). Decision (Steven): **drop it** rather than migrate.

Deleted: `source/libraries/timewarp-automation` (handlers), `source/libraries/timewarp-automation-contracts`,
`TimeWarp.Architecture/Tests/Libraries/TimeWarp.Automation.Tests`; removed from both slnx files.
Recoverable from git history if a future desktop-automation effort wants it.
