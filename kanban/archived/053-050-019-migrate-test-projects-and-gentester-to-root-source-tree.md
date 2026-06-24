# 050-019: Migrate test projects and GenTester to root source tree

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate remaining test projects and the GenTester utility from `TimeWarp.Architecture/` to the root `tests/` and `source/` directories with kebab-case naming. This is the final migration task for the source tree restructuring.

## Current State

```
TimeWarp.Architecture/Tests/
├── ContainerApps/
│   ├── Api/Api.Server.Integration.Tests/
│   ├── Aspire/Aspire.csproj
│   ├── Grpc/Grpc.Server.Integration.Tests/
│   └── Web/
│       ├── Web.Server.Integration.Tests/
│       └── Web.Spa.Integration.Tests/
├── Common/TimeWarp.Testing/
└── Libraries/TimeWarp.Automation.Tests/

TimeWarp.Architecture/Source/GenTester/
TimeWarp.Architecture/Source/Libraries/TimeWarp.Automation/
```

## Target State

```
tests/
├── container-apps/
│   ├── api/
│   │   └── api-server-integration-tests/
│   ├── aspire/
│   │   └── aspire-integration-tests/
│   ├── grpc/
│   │   └── grpc-server-integration-tests/
│   └── web/
│       ├── web-server-integration-tests/
│       └── web-spa-integration-tests/
├── common/
│   └── timewarp-testing/
└── libraries/
    └── timewarp-automation-tests/

source/
├── ... (existing)
├── gen-tester/                    ← or keep as tools/
└── libraries/
    └── timewarp-automation/       ← already listed in slnx
```

## Checklist

### Phase 1: Investigate test project structure and dependencies
- [ ] Map all test projects and their ProjectReferences
- [ ] Determine which test projects depend on which source projects (already migrated vs. not yet)
- [ ] Verify tests/ directory structure exists or needs creation

### Phase 2: Migrate common test infrastructure
- [ ] Migrate TimeWarp.Testing to tests/common/timewarp-testing/
- [ ] Update csproj and references

### Phase 3: Migrate container-app tests
- [ ] Migrate Api.Server.Integration.Tests
- [ ] Migrate Grpc.Server.Integration.Tests
- [ ] Migrate Web.Server.Integration.Tests
- [ ] Migrate Web.Spa.Integration.Tests
- [ ] Migrate Aspire test project

### Phase 4: Migrate library tests and GenTester
- [ ] Migrate TimeWarp.Automation.Tests
- [ ] Migrate GenTester (evaluate: source tree or tools?)

### Phase 5: Update and verify
- [ ] Update all ProjectReferences in test projects
- [ ] Update both solution files
- [ ] Build verify
- [ ] Run tests to verify they still pass

## Notes

- **This task should run after tasks 050-015 through 050-018** — test projects reference container apps
- Test projects may have different Directory.Build.props/Directory.Packages.props requirements
- TimeWarp.Automation is already listed in root timewarp-architecture.slnx — verify if it's already partially migrated
- GenTester may belong in tools/ rather than source/ — evaluate during task
- Consider whether tests/ needs its own Directory.Build.props chain
- Integration tests may need Aspire host running — verify test configuration after migration
## SUPERSEDED (2026-06-24) — folded into 058 + the drops

This task duplicated 058's test-migration scope (058 even flagged "coordinate with 053-050-019 so
the two don't migrate the same projects twice"). Reconciled rather than run in parallel:

- **GenTester** → **dropped** (empty Hello-World stub; would trip the System.Console ban). Not migrated.
- **TimeWarp.Automation** + **TimeWarp.Automation.Tests** → **dropped** (unused RPA demo; superseded by TimeWarp.Amuru).
- **Unit/analyzer tests** (Common.Infrastructure, Analyzers, SourceGenerator) → **done** in 058 slice 1 (migrated to root, green; SourceGen reds tracked in 058-001).
- **Testing.Common + integration tests** (Api / Web.Server / Web.Spa / Aspire) → **058 slice 3** (in progress; pre-existing breakage documented there).
- The `Grpc.Server.Integration.Tests` listed in "Current State" above **does not exist** (stale).

All remaining test-migration work lives in **058**. Archiving this as superseded.
