# 038 Methodically Update NuGet Packages

## Description

Plan and execute a coordinated set of NuGet dependency updates across the solution, informed by the latest `dotnet outdated` report. Group upgrades by risk, prioritize critical libraries, and ensure automated validation accompanies each batch of changes.

## Requirements

- Audit the `dotnet outdated` findings and categorize updates (major/minor/patch)
- Define phased upgrade plan that minimizes simultaneous high-risk changes
- Update package references in manageable batches with changelog review
- Run solution-wide build and automated test suites after each batch
- Document upgrade decisions, blockers, and follow-up items

## Checklist

### Design
- [x] Review dependency graph to understand cross-project impacts
- [x] Prioritize upgrade waves (security/compliance first)

### Implementation
- [x] Update dependencies following the phased plan
- [x] Verify builds succeed for all target frameworks
- [x] Execute automated test suites relevant to updated components
- [x] Monitor runtime smoke tests or local validation, if applicable

### Documentation
- [x] Record upgrade results and outstanding items in release notes or task log

## Completed Updates (2025-11-10)

### Wave 1: Infrastructure & Core Libraries
- OpenTelemetry packages (1.11.x → 1.13.x) - 5 packages
- FluentValidation.AspNetCore (11.3.0 → 11.3.1)
- Riok.Mapperly (4.1.1 → 4.3.0)
- libphonenumber-csharp (8.13.54 → 9.0.18) - major version
- System.ServiceModel.Primitives (8.1.1 → 8.1.2)
- TimeWarp.SourceGenerators (1.0.0-alpha.8 → 1.0.0-beta.7)
- xunit.runner.visualstudio (3.0.1 → 3.1.5)

### Wave 2: Azure & Identity
- Microsoft.Azure.AppConfiguration.AspNetCore (8.0.0 → 8.4.0)
- Azure.Identity (1.13.2 → 1.17.0)
- Microsoft.Identity.Web (3.9.2 → 4.0.1) - major version
- Microsoft.NET.Test.Sdk (17.12.0 → 18.0.0) - major version

### Test Results
- All tests passing: 25 passed (8 analyzers + 1 common + 6 API + 1 Aspire + 9 Web.Spa)
- 2 pre-existing CosmosDB emulator failures (not regression)
- 2 skipped tests
- Aspire integration test fixed and enabled in RunTests.ps1

### Additional Work
- Fixed Aspire integration test resource name ("api-server" → "api")
- Fixed Aspire integration test endpoint URL ("weatherForecasts" → "weatherforecast")
- Enabled Aspire test in RunTests.ps1

## Outstanding Items

### Aspire Packages (Deferred)
The only remaining outdated packages are Aspire-related. These should be updated together as a coordinated effort:
- Aspire.Hosting (8.2.0 → latest)
- Other Aspire.* packages currently on 9.0.0/9.5.2
- Requires testing of service orchestration and resource management

## Notes

- Reference the `dotnet outdated` output captured on 2025-11-05 for initial scope
- Highlight libraries with known breaking changes (e.g., major version jumps like FastEndpoints 5.x → 7.x)
- Coordinate with ongoing tasks (e.g., planned Mediator migration) to avoid conflicting dependency strategies
- All major version updates (Microsoft.Identity.Web, Microsoft.NET.Test.Sdk, libphonenumber-csharp) completed successfully without breaking changes
