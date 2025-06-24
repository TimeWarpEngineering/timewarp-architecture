# 033: Fix Api_Contracts Directory Naming and Semantics

## Description

Fix both the naming convention (snake_case → PascalCase) and semantic clarity of the `Api_Contracts/` documentation directory. The current name is inconsistent and semantically unclear about what it covers.

## Parent

029_Create-Directory-Naming-Convention-Analysis-Report

## Requirements

- Fix snake_case naming convention to align with project PascalCase standard
- Improve semantic clarity about what contracts this documentation covers
- Ensure name accurately reflects scope (Api.Contracts + Web.Contracts, but NOT Grpc.Contracts)
- Update any references to the old directory name

## Current Issues

**Location**: `/Documentation/Developer/HowToGuides/Api_Contracts/`

**Naming Problem**: 
- Uses snake_case when all other documentation directories use PascalCase
- Inconsistent with project naming standards

**Semantic Problem**:
- Directory name suggests it's only about `Api.Contracts` project
- Actually covers both `Api.Contracts` AND `Web.Contracts` projects
- Both deal with JSON-based HTTP APIs (REST/Web APIs)
- Excludes `Grpc.Contracts` (which uses protobuf, not JSON)
- Name doesn't clearly indicate scope

## Current Structure Analysis

**What This Documentation Covers**:
- `Api.Contracts` - JSON contracts for dedicated API service
- `Web.Contracts` - JSON contracts for Web service APIs
- Both use HTTP transport with JSON serialization
- Both follow same contract patterns and validation approaches

**What This Documentation Excludes**:
- `Grpc.Contracts` - Uses protobuf, different patterns, separate documentation needs

## Proposed Solutions

### Option 1: WebApiContracts
**Name**: `WebApiContracts/`
**Rationale**: 
- Covers both Api.Contracts and Web.Contracts (both are web APIs)
- Clear distinction from gRPC contracts
- Industry-standard "Web API" terminology

### Option 2: HttpApiContracts  
**Name**: `HttpApiContracts/`
**Rationale**:
- Emphasizes HTTP transport (vs gRPC)
- Technical precision about protocol
- Clear scope definition

### Option 3: JsonApiContracts
**Name**: `JsonApiContracts/`
**Rationale**:
- Emphasizes JSON serialization (vs protobuf)
- Clear technical distinction
- Precise about data format

### Option 4: RestApiContracts
**Name**: `RestApiContracts/`
**Rationale**:
- Traditional REST API terminology
- Industry-standard naming
- Clear architectural pattern

## Recommended Solution

**Recommendation**: `WebApiContracts/`

**Justification**:
- "Web API" is the .NET ecosystem standard term for HTTP+JSON APIs
- Clearly distinguishes from gRPC contracts
- Aligns with Microsoft terminology (.NET Web API framework)
- Covers both Api.Contracts and Web.Contracts appropriately
- Uses proper PascalCase naming

## Implementation Plan

### Phase 1: Directory Rename
- [ ] Rename `Documentation/Developer/HowToGuides/Api_Contracts/` → `Documentation/Developer/HowToGuides/WebApiContracts/`
- [ ] Verify all files within directory are preserved
- [ ] Test that documentation links still work

### Phase 2: Reference Updates
- [ ] Search for any documentation that references "Api_Contracts" directory
- [ ] Update internal links within documentation files
- [ ] Check for any build scripts or automation that references the path
- [ ] Update any README files or index pages that link to this documentation

### Phase 3: Content Review
- [ ] Review documentation content to ensure it clearly covers both Api.Contracts and Web.Contracts
- [ ] Update any content that suggests it's only about Api.Contracts
- [ ] Ensure clear distinction from gRPC contract documentation
- [ ] Add scope clarification in the directory's README/index

## Files to Check for References

**Documentation Files**:
- Any markdown files with relative links to `../Api_Contracts/`
- Index pages that list documentation sections
- README files that reference this documentation

**Configuration Files**:
- Documentation generation scripts
- Build automation that might copy or reference docs
- Any tooling configuration that lists documentation paths

## Validation Steps

- [ ] Verify directory rename successful
- [ ] Check all internal documentation links work
- [ ] Confirm no broken references to old directory name
- [ ] Verify documentation builds/generates successfully
- [ ] Test that all files are accessible at new location

## Success Criteria

1. **Consistent Naming**: Directory uses PascalCase like all other documentation directories
2. **Clear Semantics**: Name clearly indicates coverage of HTTP/JSON API contracts
3. **Accurate Scope**: Name reflects both Api.Contracts and Web.Contracts coverage
4. **Working References**: All links and references updated to new name
5. **No Broken Links**: Documentation system works with new directory name

## Risk Assessment

**Low Risk**:
- Simple directory rename
- Documentation-only change

**Medium Risk**:
- Broken internal links if not all references found
- Build scripts that hard-code the path

**Mitigation**:
- Comprehensive search for references before rename
- Test documentation generation after change
- Use git history to track exactly what changed

## Notes

This addresses both a naming convention inconsistency and a semantic clarity issue. The snake_case naming violates project standards, and the unclear scope makes it confusing what contracts this documentation covers.

**Key Decision**: Choosing `WebApiContracts/` aligns with .NET ecosystem terminology and clearly distinguishes HTTP+JSON APIs from gRPC APIs, making the documentation scope immediately clear to developers.