# 005_Create-Simplified-WeatherForecast-Endpoint.md

## Description

Create a simplified weather forecast endpoint that provides equivalent functionality to the existing GetWeatherForecastsEndpoint but without using MediatR contracts. The new endpoint will use a more direct approach while maintaining proper validation and error handling.

## Requirements

- Create new endpoint at route "api/weather"
- Accept optional "days" query parameter for forecast length
- Return weather forecast data in same format as existing endpoint
- Implement proper validation for days parameter (must be > 0)
- Handle errors appropriately with problem details
- Remove MediatR contract dependencies
- Maintain XML documentation for OpenAPI/Swagger

## Checklist

### Design
- [ ] Design simplified request/response models
- [ ] Design validation approach without FluentValidation
- [ ] Design error handling approach
- [ ] Add unit tests for new endpoint

### Implementation
- [ ] Create new WeatherEndpoint class
- [ ] Implement request/response models
- [ ] Implement parameter validation
- [ ] Implement weather forecast generation logic
- [ ] Implement error handling
- [ ] Add XML documentation
- [ ] Register endpoint in Program.cs
- [ ] Verify endpoint works through Swagger UI

### Documentation
- [ ] Add XML comments for OpenAPI documentation
- [ ] Update relevant documentation if needed

### Review
- [ ] Verify equivalent functionality to original endpoint
- [ ] Consider Performance Implications
  - Ensure direct approach doesn't impact performance
- [ ] Consider Security Implications
  - Maintain anonymous access as per original
- [ ] Code Review
  - Ensure clean, maintainable implementation
  - Verify proper error handling
  - Check documentation completeness

## Notes

Original endpoint reference:
- Source/ContainerApps/Api/Api.Server/Features/WeatherForecast/Get/GetWeatherForecastsEndpoint.cs
- Source/ContainerApps/Api/Api.Contracts/Features/WeatherForecast/Queries/GetWeatherForecasts.cs

The new implementation should maintain the same functionality but with a more direct approach:
- Remove MediatR IRequest/IRequestHandler pattern
- Simplify the request/response structure
- Keep the core weather forecast generation logic
- Maintain proper validation and error handling

## Implementation Notes

[To be filled in during implementation]
