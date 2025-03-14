# 004_003_implement-openapi-and-scalar.md

## Description

Replace SwaggerUI with Scalar UI for API documentation, using .NET's built-in OpenAPI support and Scalar.AspNetCore package.

## Parent
004_migrate-api-to-fastendpoints

## Requirements

- Remove SwaggerUI and related packages
- Add Scalar.AspNetCore package
- Configure FastEndpoints to use SwaggerDocument
- Implement OpenAPI and Scalar UI for development environment
- Update API documentation to work with the new OpenAPI implementation

## Checklist

### Design
- [x] Review current SwaggerUI implementation
- [x] Plan migration path to Scalar UI

### Implementation
- [x] Remove SwaggerUI packages and configuration
- [x] Add Scalar.AspNetCore package
- [x] Update service configuration:
  ```csharp
  services.AddFastEndpoints()
          .SwaggerDocument();
  ```
- [x] Configure OpenAPI and Scalar UI in development:
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");    
      app.MapScalarApiReference();
  }
  ```
- [ ] Verify API documentation is accessible at /scalar/v1

### Documentation
- [ ] Update API documentation to reflect new Scalar UI implementation
- [ ] Document new API documentation URL (/scalar/v1)
- [ ] Update developer documentation with new API exploration instructions

### Review
- [ ] Consider Accessibility Implications
- [ ] Consider Monitoring and Alerting Implications
- [ ] Consider Performance Implications
- [ ] Consider Security Implications
- [ ] Code Review

## Notes

Reference implementation: https://gist.github.com/dj-nitehawk/c7052f01f3f650e67fb6782c84d3b5f0

Key changes:
- Uses Scalar.AspNetCore package for modern API documentation UI
- Configures OpenAPI endpoint at /openapi/{documentName}.json
- Provides API documentation UI at /scalar/v1
- Integrates with FastEndpoints' SwaggerDocument configuration

## Implementation Notes

[2024-02-02] Initial implementation completed but encountering schema errors with Scalar UI:
- Error: "Expected string, received array"
- Error: "Example at days is invalid"
- This appears to be a known bug in Scalar when handling nullable strings: https://github.com/scalar/scalar/issues/3450
- Need to investigate further or consider alternative approaches
