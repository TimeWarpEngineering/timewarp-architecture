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
- [ ] Remove `Swashbuckle.AspNetCore` from Directory.Packages.props
- [ ] Remove Swashbuckle package references from all project files
- [ ] Verify Scalar.AspNetCore version in Directory.Packages.props (currently line 98)

### Code Changes
- [ ] Locate and remove Swashbuckle configuration in Program.cs/Startup.cs files
  - [ ] Remove `services.AddSwaggerGen()` calls
  - [ ] Remove `app.UseSwagger()` calls
  - [ ] Remove `app.UseSwaggerUI()` calls
- [ ] Remove Swashbuckle attributes from controllers/endpoints
  - [ ] Remove `[SwaggerOperation]` attributes
  - [ ] Remove `[SwaggerResponse]` attributes
  - [ ] Remove other Swashbuckle-specific attributes
- [ ] Remove custom Swashbuckle filters and configurations
- [ ] Add Scalar configuration
  - [ ] Add `app.MapScalarApiReference()` or equivalent
  - [ ] Configure Scalar options as needed

### Testing
- [ ] Build solution to verify no compilation errors
- [ ] Run application(s) and verify API documentation UI loads
- [ ] Test API documentation functionality
- [ ] Verify all endpoints are properly documented
- [ ] Run test suites to ensure no regressions

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

[To be updated during task execution]

## Definition of Done

- Swashbuckle.AspNetCore completely removed from solution
- Scalar.AspNetCore properly configured and functional
- All tests passing
- API documentation accessible and functional
- Documentation updated
