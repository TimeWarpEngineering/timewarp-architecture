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
- [ ] Review dependency graph to understand cross-project impacts
- [ ] Prioritize upgrade waves (security/compliance first)

### Implementation
- [ ] Update dependencies following the phased plan
- [ ] Verify builds succeed for all target frameworks
- [ ] Execute automated test suites relevant to updated components
- [ ] Monitor runtime smoke tests or local validation, if applicable

### Documentation
- [ ] Record upgrade results and outstanding items in release notes or task log

## Notes

- Reference the `dotnet outdated` output captured on 2025-11-05 for initial scope
- Highlight libraries with known breaking changes (e.g., major version jumps like FastEndpoints 5.x → 7.x)
- Coordinate with ongoing tasks (e.g., planned Mediator migration) to avoid conflicting dependency strategies
