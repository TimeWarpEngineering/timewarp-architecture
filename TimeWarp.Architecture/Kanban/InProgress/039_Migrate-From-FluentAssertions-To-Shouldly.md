# 039 Migrate From FluentAssertions To Shouldly

## Description

Replace FluentAssertions with Shouldly across all test projects. Shouldly provides more readable assertion messages and aligns with modern testing practices. This migration will improve test maintainability and debugging experience.

## Requirements

- Identify all test files using FluentAssertions (approximately 8 files across 7 test projects)
- Convert FluentAssertions syntax to equivalent Shouldly assertions
- Update using statements in all affected test files
- Remove FluentAssertions package references from test projects
- Verify all tests pass after migration
- Remove FluentAssertions from Directory.Packages.props once fully migrated

## Checklist

### Design
- [ ] Audit all test files to identify FluentAssertions usage patterns
- [ ] Create mapping guide for common assertion conversions (e.g., `.Should().Be()` → `.ShouldBe()`)
- [ ] Identify any complex FluentAssertions patterns that need special handling

### Implementation
- [ ] Update test files to use Shouldly syntax
- [ ] Replace `using FluentAssertions;` with `using Shouldly;`
- [ ] Remove FluentAssertions package references from individual .csproj files
- [ ] Run all test suites to verify conversions are correct (`./RunTests.ps1`)
- [ ] Remove FluentAssertions from Directory.Packages.props

### Documentation
- [ ] Update any testing documentation that references FluentAssertions
- [ ] Add notes about preferred assertion library for future contributors

## Notes

- Shouldly is already in Directory.Packages.props (version 4.3.0)
- Common conversions:
  - `.Should().Be(expected)` → `.ShouldBe(expected)`
  - `.Should().BeTrue()` → `.ShouldBeTrue()`
  - `.Should().NotBeNull()` → `.ShouldNotBeNull()`
  - `.Should().BeEquivalentTo()` → `.ShouldBeEquivalentTo()`
- Consider doing this migration in batches by test project to minimize risk
- Coordinate with task 038 (NuGet updates) to avoid version conflicts
