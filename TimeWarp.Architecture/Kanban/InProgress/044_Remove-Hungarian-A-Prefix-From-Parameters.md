# 044 Remove Hungarian 'a' Prefix From Parameters

## Description

Remove legacy Hungarian notation 'a' prefix from parameters and local variables across the codebase. This pattern (e.g., `aValue`, `aName`, `aRequest`) is inconsistent with modern C# conventions which use camelCase without prefixes. The refactoring will improve code readability and align with current .NET standards.

## Requirements

- Remove 'a' prefix from 134 identified instances across 58 files
- Ensure parameter names follow standard camelCase convention (e.g., `aValue` → `value`)
- Maintain code functionality - no breaking changes to public APIs
- Run full test suite after refactoring to verify no regressions
- Prioritize changes starting with core libraries that propagate to other areas

## Scope

**Total Impact**: 134 matches across 58 files
- Common libraries (Enumeration.cs, BaseEndpoint.cs, extensions): ~40%
- Web/Grpc services (Program.cs, handlers, services): ~30%
- Tests (Integration tests, test infrastructure): ~20%
- Other (Configurations, states, validators): ~10%

## Checklist

### Design
- [x] Review analysis report identifying all affected locations
- [ ] Determine refactoring strategy (automated vs manual review)
- [ ] Identify high-risk areas requiring careful review (lambdas, delegates)

### Implementation - Phase 1: Core Libraries
- [x] Source/Common/Common.Domain/Enumeration/Enumeration.cs (12 matches)
- [x] Source/Common/Common.Server/CommonServerModule.cs (3 matches)
- [x] Source/Common/Common.Server/IAspNetModule.cs (4 matches)
- [x] Source/Common/Common.Server/Base/BaseEndpoint.cs (1 match)
- [x] Source/Common/Common.Server/Extensions/MvcBuilderExtensions.cs (5 matches)
- [x] Source/Common/Common.Server/CorsPolicy/*.cs (13 matches across 3 files)

### Implementation - Phase 2: Container Apps
- [x] Source/ContainerApps/Api/Api.Server/Features/Base/*.cs (2 matches)
- [x] Source/ContainerApps/Grpc/Grpc.Server/**/*.cs (19 matches across 5 files)
- [x] Source/ContainerApps/Grpc/Grpc.Contracts/**/*.cs (4 matches across 2 files)
- [ ] Source/ContainerApps/Web/Web.Server/Program.cs (9 matches)
- [x] Source/ContainerApps/Web/Web.Spa/Program.cs (6 matches)
- [x] Source/ContainerApps/Web/Web.Application/**/*.cs (2 matches)
- [ ] Source/ContainerApps/Web/Web.Spa/Features/**/*.cs (19 matches across 9 files)
- [ ] Source/ContainerApps/Web/Web.Spa/Pipeline/*.cs (8 matches across 3 files)
- [ ] Source/ContainerApps/Web/Web.Spa/Components/Forms/*.cs (6 matches)
- [ ] Source/ContainerApps/Web/Web.Infrastructure/**/*.cs (6 matches across 2 files)
- [ ] Source/ContainerApps/Web/Web.Contracts/**/*.cs (3 matches across 2 files)
- [ ] Source/ContainerApps/Web/Web.Server/**/*.cs (6 matches across 3 files)
- [ ] Source/ContainerApps/Api/Api.Application/**/*.cs (3 matches across 2 files)

### Implementation - Phase 3: Tests
- [ ] Tests/Libraries/TimeWarp.Automation.Tests/*.cs (3 matches)
- [ ] Tests/ContainerApps/Web/Web.Spa.Integration.Tests/**/*.cs (14 matches across 7 files)
- [ ] Tests/ContainerApps/Api/Api.Server.Integration.Tests/**/*.cs (2 matches)
- [ ] Tests/ContainerApps/Web/Web.Server.Integration.Tests/**/*.cs (8 matches across 4 files)
- [ ] Tests/TimeWarp.Testing/**/*.cs (18 matches across 6 files)

### Validation
- [ ] Run solution-wide build without errors
- [ ] Execute full test suite - verify all tests pass
- [ ] Review lambda expressions and delegates for correctness
- [ ] Spot-check refactored code for logical errors
- [ ] Verify no breaking changes to public APIs

### Documentation
- [ ] Update any documentation referencing the old naming pattern
- [ ] Document refactoring completion in task notes

## High-Risk Areas Requiring Manual Review

1. **Lambda expressions**: Ensure parameter renames don't break closure semantics
2. **Delegates and callbacks**: Verify delegate signatures still match
3. **Constructor parameter to field assignments**: Ensure correct mapping maintained
4. **LINQ expressions**: Check query syntax correctness after rename
5. **FluentValidation rules**: Verify validator lambda expressions

## Refactoring Strategy

### Automated Approach
- Use IDE refactoring tools (Rider/VS Code) for safe rename operations
- Process files in batches by phase
- Run tests after each phase to catch issues early

### Manual Review Cases
- Lambda parameters in complex LINQ queries
- Delegate parameters with multiple overloads
- Generic method type parameters named with 'a' prefix
- Constructor parameters assigned to fields

## Progress Notes

### 2025-11-11: Phase 1 - Core Libraries (In Progress)

**Enumeration.cs Completed**
- Refactored all 12 instances in Enumeration.cs
- Changed: `aValue`, `aName`, `aAlternateCodes`, `aItem`, `aOther`, `anObject`, `aPredicate`, `aDescription` → camelCase without prefix
- Preserved: `aString` (kept to avoid `string` keyword collision)
- All lambda parameters updated consistently
- Manual review completed - no breaking changes

**CommonServerModule.cs Completed**
- Refactored all 3 instances
- Changed: `aHttpContext`, `aAzureAppConfigurationOptions`, `aAzureAppConfigurationRefreshOptions`, `aAzureAppConfigurationKeyVaultOptions` → camelCase without prefix
- Lambda parameters in configuration fluent API updated
- Manual review completed - no breaking changes

**IAspNetModule.cs Completed**
- Refactored all 4 instances
- Changed: `aConfigurationManager`, `aWebApplication` → camelCase without prefix
- Static abstract interface method signatures updated
- Manual review completed - no breaking changes

**BaseEndpoint.cs Completed**
- Refactored 1 instance
- Changed: `aRequest` → `request`
- Generic base endpoint Send method updated
- Manual review completed - no breaking changes

**MvcBuilderExtensions.cs Completed**
- Refactored all 5 instances
- Changed: `aMvcBuilder`, `aAssembly`, `aApplicationPartManager`, `aAssemblyPart` → camelCase without prefix
- Extension method and lambda parameters updated
- Manual review completed - no breaking changes

**CorsPolicy Files Completed (CorsPolicy.cs, ExamplePolicy.cs, AnyPolicy.cs)**
- Refactored all 13 instances across 3 files
- Changed: `aValue`, `aName`, `aAlternateCodes`, `aServiceCollection`, `aOptions`, `aBuilder`, `aWebApplication` → camelCase without prefix
- Method parameters, lambda parameters, and XML documentation examples updated
- Manual review completed - no breaking changes

### Phase 1 Complete! ✅
All 38 instances in Common libraries have been successfully refactored.

### 2025-11-11: Phase 2 - Container Apps (In Progress)

**API Server Base Classes Completed**
- BaseException.cs: `aMessage` → `message`
- BaseError.cs: `aMessage` → `message`
- Manual review completed - no breaking changes

**gRPC Server and Contract Files Completed**
- HelloService.cs: `aHelloRequest`, `aCallContext` → `helloRequest`, `callContext`
- ProtobufGenerationHostedService.cs: `aServiceProvider`, `aLogger`, `aCancellationToken` → camelCase without prefix
- SuperheroService.cs: `aLength`, `aSuperheroRequest`, `aCallContext` → camelCase without prefix
- Program.cs: `aServiceCollection`, `aWebApplication` → `serviceCollection`, `webApplication`
- GreeterService.cs: `aHelloRequest`, `aServerCallContext` → `helloRequest`, `serverCallContext`
- IHelloService.cs: `aHelloRequest`, `aCallContext` → `helloRequest`, `callContext`
- ISuperheroService.cs: `aSuperheroRequest`, `aCallContext` → `superheroRequest`, `callContext`
- Manual review completed - no breaking changes

**Web Application Handler Completed**
- TrackEvent.Handler.cs: `aTrackEventRequest`, `aCcancellationToken` → `trackEventRequest`, `cancellationToken`
- Manual review completed - no breaking changes

**Web.Spa Program.cs Completed**
- Program.cs: `aJsonSerializerOptions`, `aValidationConfiguration`, `aServiceDescriptor` → camelCase without prefix
- JSON serializer configuration lambda updated
- Commented validation code also updated for consistency
- Manual review completed - no breaking changes

## Notes

- Analysis report generated: 2025-11-11 (see .agent/workspace/a-prefix-parameters-report.md)
- Search criteria: Regex `\ba[A-Z]` on `*.cs` files
- Priority areas: Common.* libraries affect multiple downstream projects
- Related to broader naming convention standardization effort
- This is part of codebase modernization and consistency improvements
- Exception: Parameters may retain 'a' prefix when needed to avoid keyword collisions (e.g., `aString` to avoid `string`)

## References

- Analysis Report: [a-prefix-parameters-report.md](../../.agent/workspace/a-prefix-parameters-report.md)
- Related Task: 027_Create-File-Naming-Convention-ADR-And-Documentation.md
