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
- [x] Review OneOf serialization documentation
- [x] Design serialization strategy for OneOf types
- [ ] Add/Update Tests for serialization scenarios

### Implementation
- [x] ~~Implement custom JsonConverter for OneOf types if needed~~ (Not needed)
- [x] ~~Configure System.Text.Json to use the correct serialization handling~~ (Direct response writing used instead)
- [x] Update BaseFastEndpoint to handle OneOf serialization properly
- [ ] Verify both success and error responses serialize correctly

### Documentation
- [x] Document OneOf serialization configuration
- [ ] Update API documentation if needed

### Review
- [x] Consider Performance Implications of serialization changes
- [ ] Code Review
- [ ] Test with different response scenarios

## Implementation Notes

The serialization issue was resolved by:
1. Removing the use of SendAsync in BaseFastEndpoint which was causing the OneOf wrapper to be serialized
2. Using WriteAsJsonAsync to directly write the success/error response objects
3. Properly handling async/await patterns in the response handling
4. Setting appropriate content types for both success and error responses

This approach:
- Avoids the need for custom JSON converters
- Maintains proper response structure
- Handles both success and error cases correctly
- Uses OneOf's Match pattern for type-safe response handling

### Future Investigation Note

While the current solution works by directly writing responses, there may be a way to use FastEndpoints' SendAsync method (the more idiomatic approach). The issue might be related to type parameter handling:

1. BaseFastEndpoint declares `OneOf<TResponse, SharedProblemDetails>` in its type parameters
2. But the generated endpoint (GetWeatherForecastsEndpoint) inherits from `BaseFastEndpoint<Query, Response>`
3. This type mismatch might be causing SendAsync to get confused about the response type

This could be investigated in a future task after the test infrastructure is fixed.

## Notes

Related files:
- Source/ContainerApps/Api/Api.Contracts/Features/WeatherForecast/Queries/GetWeatherForecasts.cs
- Source/ContainerApps/Api/Api.Application/Features/WeatherForecast/GetWeatherForecastshandler.cs
- Source/Common/Common.Server/Base/BaseFastEndpoint.cs

The issue appears to be in the serialization layer rather than the business logic, as the handler is correctly creating and returning the OneOf response.

## Testing Notes

The solution has existing integration tests in `GetWeatherForecastsEndpoint_Aspire_Tests.cs` that cover:
1. Success case: Requesting 10 days of weather forecasts
2. Error case: Triggering validation error with negative days

However, the tests are currently failing due to a missing ConfigureServices method in the Program class:
```
D:\git\github\TimeWarpEngineering\timewarp-architecture\TimeWarp.Architecture\Tests\TimeWarp.Testing\Applications\SpaTestApplication.cs(32,13): error CS0117: 'Program' does not contain a definition for 'ConfigureServices'
```

A new task (010_Fix-SpaTestApplication-ConfigureServices) has been created to:
1. Fix the ConfigureServices method issue
2. Get the test infrastructure working properly
3. Allow verification of the OneOf serialization fix

## Current Status

1. ✅ Implemented OneOf serialization fix in BaseFastEndpoint
2. ✅ Created task 010 to fix test infrastructure
3. ⏳ Keeping this task in InProgress until we can verify the fix through tests
4. 🔄 Will move to Done once tests confirm the solution works
