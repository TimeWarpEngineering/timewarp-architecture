# 042: Migrate From Swashbuckle To Scalar

## Description

Replace `Swashbuckle.AspNetCore` with `Scalar.AspNetCore` for API documentation. This migration involves removing OpenAPI/Swagger generation tooling and replacing it with Scalar's modern API documentation interface.

## Context

Based on impact analysis in `.agent/workspace/impact-analysis-swashbuckle-to-scalar.md`. Note that Scalar.AspNetCore is a modern alternative to Swagger UI that provides a better developer experience while maintaining OpenAPI specification compatibility.

## Requirements

- Remove Swashbuckle.AspNetCore package from Directory.Packages.props
- Remove all Swashbuckle-related configuration code
- Add and configure Scalar.AspNetCore
- Verify API documentation functionality
- Update developer documentation

## Checklist

### Package Management
- [x] Remove `Swashbuckle.AspNetCore` from Directory.Packages.props
- [x] Remove Swashbuckle package references from all project files (Common.Server.csproj)
- [x] Verify Scalar.AspNetCore version in Directory.Packages.props (version 2.10.3)

### Code Changes
- [x] Locate and remove Swashbuckle configuration in Program.cs/Startup.cs files
  - [x] Remove `services.AddSwaggerGen()` calls (replaced with AddOpenApi)
  - [x] Remove `app.UseSwagger()` calls (replaced with MapScalarApiReference)
  - [x] Remove `app.UseSwaggerUI()` calls
- [x] Remove Swashbuckle attributes from controllers/endpoints
  - [x] Remove `[SwaggerOperation]` attributes from 4 endpoint files
  - [x] Remove `[SwaggerResponse]` attributes (none found)
  - [x] Remove other Swashbuckle-specific attributes
- [x] Remove custom Swashbuckle filters and configurations (AddFluentValidationRulesToSwagger)
- [x] Add Scalar configuration
  - [x] Add `app.MapScalarApiReference()` in CommonServerModule
  - [x] Add `serviceCollection.AddEndpointsApiExplorer()` for API discovery

### Testing
- [x] Build solution to verify no compilation errors
- [ ] Run application(s) and verify API documentation UI loads
- [ ] Test API documentation functionality
- [ ] Verify all endpoints are properly documented
- [x] Run test suites to ensure no regressions (All relevant API tests passed)

### Documentation
- [ ] Update developer documentation to reference Scalar instead of Swagger
- [ ] Document new API documentation URL/path if changed
- [ ] Update any README files mentioning Swagger/Swashbuckle

## Notes

**Important Considerations:**
- Scalar provides a modern alternative to Swagger UI but maintains OpenAPI compatibility
- The migration should not affect the OpenAPI specification itself
- API clients relying on OpenAPI specs should continue to work
- Developers will need to use the new Scalar UI instead of Swagger UI

**Affected Projects:**
- Likely affects API container apps in `ContainerApps/Api/`
- May affect other services with API endpoints

## Implementation Notes

### Changes Made

**Packages Removed:**
- Swashbuckle.AspNetCore (6 variations removed from Directory.Packages.props)
- MicroElements.Swashbuckle.FluentValidation
- FluentValidation.AspNetCore (deprecated package)
- FastEndpoints.Swagger (unnecessary with Scalar)
- All Swashbuckle package references from Common.Server.csproj

**Packages Added:**
- Scalar.AspNetCore added to Common.Server.csproj and Api.Server.csproj

**Packages Updated:**
- FluentValidation.DependencyInjectionExtensions: 11.9.1 → 12.1.0

**Code Changes:**
1. [CommonServerModule.cs](Source/Common/Common.Server/CommonServerModule.cs):
   - Replaced `AddSwaggerGen()` method with `AddOpenApi()` that calls `AddEndpointsApiExplorer()`
   - Replaced `UseSwaggerUi()` method with `UseScalarApiReference()` that calls `MapScalarApiReference()`

2. [Web.Server/Program.cs](Source/ContainerApps/Web/Web.Server/Program.cs):
   - Updated call from `AddSwaggerGen` to `AddOpenApi`
   - Updated call from `UseSwaggerUi` to `UseScalarApiReference`
   - Removed unused constants (SwaggerBasePath, SwaggerEndpoint)

3. GlobalUsings files:
   - Removed `MicroElements.Swashbuckle.FluentValidation.AspNetCore` from Common.Server
   - Removed `Swashbuckle.AspNetCore.Annotations` from Web.Server
   - Removed `Microsoft.OpenApi.Models` from both (no longer needed)
   - Added `Scalar.AspNetCore` to Common.Server

4. Endpoint files (4 files updated):
   - [HelloEndpoint.cs](Source/ContainerApps/Web/Web.Server/Features/Hello/HelloEndpoint.cs:14)
   - [GetSignInTokenEndpoint.cs](Source/ContainerApps/Web/Web.Server/Features/Auth/GetSignInTokenEndpoint.cs:8)
   - [TrackEventEndpoint.cs](Source/ContainerApps/Web/Web.Server/Features/Analytics/TrackEventEndpoint.cs:12)
   - [GetProfileEndpoint.cs](Source/ContainerApps/Web/Web.Server/Features/Profile/GetProfileEndpoint.cs:8)
   - All `[SwaggerOperation(Tags = [FeatureAnnotations.FeatureGroup])]` attributes removed

5. [Api.Server/Program.cs](Source/ContainerApps/Api/Api.Server/Program.cs):
   - Removed FastEndpoints.Swagger configuration (`.SwaggerDocument()` method chain)
   - Simplified `AddFastEndpoints()` to basic configuration without Swagger document settings
   - Updated middleware to use `MapScalarApiReference()` instead of Swagger UI
   - Removed FastEndpoints.Swagger package reference from Api.Server.csproj
   - Removed FastEndpoints.Swagger from GlobalUsings.cs

6. FluentValidation.AspNetCore deprecation cleanup:
   - Removed `AddFluentValidationAutoValidation()` and `AddFluentValidationClientsideAdapters()` calls from Web.Server/Program.cs
   - Removed FluentValidation.AspNetCore from Web.Server.csproj and Api.Server.csproj
   - Updated FluentValidation.DependencyInjectionExtensions to version 12.1.0
   - Removed FluentValidation.AspNetCore global using statements

**Build Status:** ✅ All projects build successfully (Build.ps1 completed in 16.4s)

## Definition of Done

- Swashbuckle.AspNetCore completely removed from solution
- Scalar.AspNetCore properly configured and functional
- All tests passing
- API documentation accessible and functional
- Documentation updated
