# 012_Expand-RunTests-Script.md

## Description

Modify the RunTests.ps1 script to explicitly run each Tests project instead of using the wildcard pattern `*.Tests`. This will provide better control and visibility over which test projects are being executed.

## Requirements

- Update RunTests.ps1 to explicitly list and run each test project
- Maintain the current Push-Location/Pop-Location pattern for directory management
- Ensure all test projects in the solution are included
- Test projects to be included:
  - Tests/Analyzers/*
  - Tests/Common/*
  - Tests/ContainerApps/*
  - Tests/EndToEnd.Playwright.Tests
  - Tests/Libraries/*
  - Tests/TimeWarp.Testing
  - Tests/Web.Server.Integration.Tests
  - Tests/Web.Spa.Integration.Tests

## Checklist

### Design
- [ ] Review all test projects in the solution
- [ ] Design script structure for explicit test execution

### Implementation
- [ ] Update RunTests.ps1 script
- [ ] Test the updated script
- [ ] Verify all test projects are executed

### Documentation
- [ ] Update comments in RunTests.ps1 to explain the explicit test execution approach
- [ ] Update any relevant documentation referencing the test execution process

### Review
- [ ] Consider Performance Implications
  - Evaluate if explicit test execution affects overall test run time
- [ ] Code Review
  - Ensure script modifications follow PowerShell best practices
  - Verify all test projects are included

## Notes

Current implementation in RunTests.ps1:
```powershell
Push-Location $PSScriptRoot
try {
    dotnet fixie *.Tests
}
finally {
    Pop-Location
}
```

This change will make test execution more explicit and maintainable, allowing for better control over which test projects are run and in what order.

## Implementation Notes

[To be filled during implementation]
