# Share API transport core between SPA BaseApiService and TestApiService

## Parent

131

## Description

Extract the shared HTTP transport core mirrored between SPA `BaseApiService` and test
`TestApiService` (task 131 F-015 extract half). Interim SPA catch/verb alignment was done
under 131; this task removes the remaining dual-maintenance mirror.

## Requirements

- Shared transport core (HttpClient + seam `JsonSerializerOptions` + token-acquisition
  delegate) both sides compose.
- Tests must not take a ProjectReference on the SPA stack.
- Keep verb matrix consistent (`NotSupportedException` for unsupported verbs).
- Do not reintroduce swallow-all exception catch on problem deserialization.

## Checklist

- [x] Choose home assembly — foundation-contracts / HttpApiService
- [x] Extract and compose BaseApiService + TestApiService
- [x] Async per-request token seam for SPA IAccessTokenProvider
- [x] Head/Options → NotSupportedException
- [x] Stream/FileResponse single path; 204 before success
- [x] Design regions rewritten on core + composers
- [x] EndpointEmitModel equatability comment fixed
- [x] foundation-contracts-tests 13 passed; identity registration suite green
- [x] Phase 4b review disposition clean

## Notes

### Implementation plan (2026-07-29)

Executed: `HttpApiService` in foundation-contracts; SPA/Test composers; rider comment.

## Session

- Created: 2026-07-28 — from task 131 disposition
- Plan: 2026-07-29 — tw-orchestrate-task Phase 2/3
- Implement: 2026-07-29 — Phase 4 (`69a57391`)
- Review: 2026-07-29 — Phase 4b general, disposition clean

## Results

**What shipped**
- `source/foundation/foundation-contracts/services/http-api-service.cs` — shared WASM-safe
  HTTP transport for `IApiService`.
- SPA `BaseApiService` / `BaseAuthApiService` thin composers; single MSAL acquire path.
- `TestApiService` composes core; ctor bearer pin unchanged; raw HTTP still public.
- Unit tests: `tests/foundation/foundation-contracts-tests/http-api-service-tests.cs`.
- Intentional fix: 204 handled before success branch (old 204 arm was dead under IsSuccessStatusCode).
- EndpointEmitModel Design: honest ImmutableArray equality note.

**Tests:** foundation-contracts-tests **13 passed**; AgentRegistration-related **14 passed**.

**Review:** effort 1 general; **0 open**; disposition **clean**. Paths under `review/`.
