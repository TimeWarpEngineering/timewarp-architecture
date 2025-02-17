# 009_Fix-OneOf-Serialization-WeatherForecast.md

## Description

Fix serialization issue in WeatherForecast endpoint where OneOf<Response, SharedProblemDetails> is failing to serialize properly. The error occurs when the endpoint tries to serialize a successful Response (T0) but the serializer attempts to access it as SharedProblemDetails (T1).

Error details:
```
System.InvalidOperationException: Cannot return as T1 as result is T0
   at OneOf.OneOf`2.get_AsT1()
   at System.Text.Json.Serialization.Metadata.JsonPropertyInfo`1.GetMemberAndWriteJson
```

## Requirements

1. Implement proper System.Text.Json serialization handling for OneOf types
2. Ensure successful responses (T0) are serialized correctly
3. Ensure error responses (T1) are serialized correctly
4. Maintain existing API contract and response structure
5. Add tests to verify serialization behavior

## Checklist

### Design
- [ ] Review OneOf serialization documentation
- [ ] Design serialization strategy for OneOf types
- [ ] Add/Update Tests for serialization scenarios

### Implementation
- [ ] Implement custom JsonConverter for OneOf types if needed
- [ ] Configure System.Text.Json to use the correct serialization handling
- [ ] Update BaseFastEndpoint to handle OneOf serialization properly
- [ ] Verify both success and error responses serialize correctly

### Documentation
- [ ] Document OneOf serialization configuration
- [ ] Update API documentation if needed

### Review
- [ ] Consider Performance Implications of serialization changes
- [ ] Code Review
- [ ] Test with different response scenarios

## Notes

Related files:
- Source/ContainerApps/Api/Api.Contracts/Features/WeatherForecast/Queries/GetWeatherForecasts.cs
- Source/ContainerApps/Api/Api.Application/Features/WeatherForecast/GetWeatherForecastshandler.cs
- Source/Common/Common.Server/Base/BaseFastEndpoint.cs

The issue appears to be in the serialization layer rather than the business logic, as the handler is correctly creating and returning the OneOf response.

## Implementation Notes

Potential solutions to investigate:
1. Custom JsonConverter for OneOf types
2. Middleware to handle OneOf type unwrapping before serialization
3. Update FastEndpoints configuration for proper OneOf handling
