# 010_Fix-SpaTestApplication-ConfigureServices.md

## Description

Fix the test infrastructure error in SpaTestApplication where the Program class is missing a ConfigureServices method. This is causing integration tests to fail with the error:

```
D:\git\github\TimeWarpEngineering\timewarp-architecture\TimeWarp.Architecture\Tests\TimeWarp.Testing\Applications\SpaTestApplication.cs(32,13): error CS0117: 'Program' does not contain a definition for 'ConfigureServices'
```

## Parent
009_Fix-OneOf-Serialization-WeatherForecast

## Requirements

1. Fix the ConfigureServices method missing from Program class
2. Ensure test infrastructure properly initializes services
3. Verify integration tests can run successfully
4. Maintain compatibility with existing test patterns

## Checklist

### Design
- [ ] Review SpaTestApplication architecture
- [ ] Identify correct service configuration pattern
- [ ] Plan minimal changes needed to fix the issue

### Implementation
- [ ] Add ConfigureServices method to Program class
- [ ] Update SpaTestApplication to use correct configuration
- [ ] Verify test infrastructure initializes properly
- [ ] Run integration tests to confirm fix

### Documentation
- [ ] Document service configuration approach
- [ ] Update test infrastructure documentation
- [ ] Add comments explaining configuration pattern

### Review
- [ ] Consider test startup performance
- [ ] Review service configuration
- [ ] Code Review

## Notes

Related files:
- Tests/TimeWarp.Testing/Applications/SpaTestApplication.cs
- Source files containing Program class definition

The error suggests a mismatch between the test infrastructure's expectations and the actual Program class implementation. This needs to be fixed before we can verify the OneOf serialization changes.

## Implementation Notes

The fix needs to:
1. Align with current .NET service configuration patterns
2. Support the test infrastructure's needs
3. Allow proper initialization of services for integration tests
