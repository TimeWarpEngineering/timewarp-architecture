# 004_002_remove-controllers-from-api.md

## Description

Remove all controllers from the API project since we are using FastEndpoints instead. This is part of the API migration to FastEndpoints architecture.

## Parent
004_migrate-api-to-fastendpoints

## Requirements

- Remove all controller classes from the API project
- Remove any controller-specific configurations and middleware
- Ensure all endpoints are properly implemented using FastEndpoints before removing their controller counterparts
- Update API documentation to reflect the FastEndpoints architecture

## Checklist

### Design
- [ ] Identify all existing controllers
- [ ] Verify all controller functionality has FastEndpoints equivalents

### Implementation
- [ ] Remove controller classes
- [ ] Remove controller-specific configurations from Program.cs and other startup files
- [ ] Remove any unused controller-related packages/dependencies
- [ ] Remove controller-specific middleware
- [ ] Verify API functionality after controller removal

### Documentation
- [ ] Update API documentation to reflect FastEndpoints architecture
- [ ] Remove any controller-specific documentation

### Review
- [ ] Consider Accessibility Implications
- [ ] Consider Monitoring and Alerting Implications
- [ ] Consider Performance Implications
- [ ] Consider Security Implications
- [ ] Code Review

## Notes

This task is part of the migration to FastEndpoints architecture. Controllers should only be removed after verifying that their functionality has been properly implemented using FastEndpoints.

## Implementation Notes

[Include notes while task is in progress]
