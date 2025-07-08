# 037 Migrate From MediatR To TimeWarp.Mediator

## Description

Migrate the codebase from using MediatR to TimeWarp.Mediator. Since TimeWarp.Mediator is a fork of Mediator (forked at the last Apache 2 license commit before it went commercial), this should be a straightforward migration involving mainly package references and global usings changes. Create a migration document with notes during the process.

## Requirements

- Replace all MediatR package references with TimeWarp.Mediator
- Update global usings to reference TimeWarp.Mediator namespaces
- Create comprehensive migration documentation with notes and lessons learned
- Ensure all existing functionality continues to work after migration
- Verify all tests pass after migration

## Checklist

### Design
- [ ] Analyze current MediatR usage patterns across the solution
- [ ] Identify all package references and global usings to update
- [ ] Plan migration strategy focusing on package refs and usings

### Implementation
- [ ] Update package references in all .csproj files
- [ ] Update global usings files to reference TimeWarp.Mediator namespaces
- [ ] Update any explicit using statements if needed
- [ ] Update dependency injection registrations if namespace changes affect them
- [ ] Run build to verify no compilation errors
- [ ] Run all tests to ensure functionality is maintained

### Documentation
- [ ] Create migration document with step-by-step notes
- [ ] Document any issues encountered and solutions
- [ ] Update CLAUDE.md references from MediatR to TimeWarp.Mediator
- [ ] Document benefits of using TimeWarp.Mediator over MediatR

## Notes

- TimeWarp.Mediator is a fork of Mediator (not MediatR) from the last Apache 2 license commit
- This should be primarily a package reference and namespace change
- The migration document should serve as a guide for future similar migrations
- Pay attention to any subtle differences between the forked version and original
- Document the reasoning behind the fork (commercial licensing issue)