# 040 Configure Aspire Test Host Api Server

## Description

Ensure the Aspire-hosted integration test environment registers the `api-server` resource with a valid base address so Web API integration tests can execute without runtime failures.

## Requirements

- Provide a stable base URI for the `api-server` resource used by integration tests
- Confirm the test harness uses the configured base address when issuing HTTP requests
- Re-run the API Server integration test suite to verify the Aspire scenarios succeed

## Checklist

### Implementation
- [ ] Update Aspire test host configuration so `api-server` is discoverable by tests
- [ ] Verify the integration tests reach the API without `InvalidOperationException`

## Notes

- Current failures occur in `Api.Server.Integration.Tests` when `WebApiTestService` cannot resolve an absolute URI.
