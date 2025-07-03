# B003: Build Flow to Check @key on Blazor Loops

## Description
Create a Flow (validation/analysis tool) to automatically detect Blazor loops that are missing the `@key` directive and ensure proper key usage for performance and correctness.

## Background
Blazor loops without proper `@key` directives can cause rendering issues, performance problems, and incorrect component state management when the collection changes.

## Requirements
- Detect `@for`, `@foreach` loops in Blazor components
- Identify loops missing `@key` directive
- Validate that `@key` values are unique and stable
- Report locations of violations with line numbers
- Integrate with existing build/validation pipeline
- Provide clear error messages with remediation guidance

## Checklist

### Design
- [ ] Design loop detection algorithm for .razor files
- [ ] Define validation rules for @key usage
- [ ] Plan integration with build pipeline
- [ ] Design error reporting format

### Implementation
- [ ] Create Roslyn analyzer or custom parser
- [ ] Implement loop detection logic
- [ ] Add @key validation rules
- [ ] Build error reporting system
- [ ] Add build pipeline integration

### Documentation
- [ ] Create usage documentation
- [ ] Document configuration options
- [ ] Add troubleshooting guide

## Notes

Should analyze `.razor` files and consider both server-side and WebAssembly scenarios. May leverage Roslyn analyzers or custom parsing. Should handle nested loops appropriately.

Priority: Medium - Code quality and performance improvement

Labels: flow, blazor, validation, performance, code-quality