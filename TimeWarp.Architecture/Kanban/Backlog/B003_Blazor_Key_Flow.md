# B044: Build Flow to Check @key on Blazor Loops

## Description
Create a Flow (validation/analysis tool) to automatically detect Blazor loops that are missing the `@key` directive and ensure proper key usage for performance and correctness.

## Background
Blazor loops without proper `@key` directives can cause rendering issues, performance problems, and incorrect component state management when the collection changes.

## Acceptance Criteria
- [ ] Detect `@for`, `@foreach` loops in Blazor components
- [ ] Identify loops missing `@key` directive
- [ ] Validate that `@key` values are unique and stable
- [ ] Report locations of violations with line numbers
- [ ] Integrate with existing build/validation pipeline
- [ ] Provide clear error messages with remediation guidance

## Technical Notes
- Should analyze `.razor` files
- Consider both server-side and WebAssembly scenarios
- May leverage Roslyn analyzers or custom parsing
- Should handle nested loops appropriately

## Priority
Medium - Code quality and performance improvement

## Labels
- flow
- blazor
- validation
- performance
- code-quality