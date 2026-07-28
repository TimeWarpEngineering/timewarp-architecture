# Share API transport core between SPA BaseApiService and TestApiService

## Parent

131

## Description

Extract the shared HTTP transport core mirrored between SPA `BaseApiService` and test
`TestApiService` (task 131 F-015 extract half). Interim SPA catch/verb alignment was done
under 131; this task removes the remaining dual-maintenance mirror.

## Requirements

- Shared transport core (HttpClient + seam `JsonSerializerOptions` + token-acquisition
  delegate) both sides compose — home likely foundation client seam types already shared
  (`IApiService`, `IApiRequest`, `SharedProblemDetails`, `ContractSerializationDefaults`).
- Tests must not take a ProjectReference on the SPA stack (flag-combination constraint).
- Keep verb matrix consistent (`NotSupportedException` for unsupported verbs).
- Do not reintroduce swallow-all exception catch on problem deserialization.

## Checklist

- [ ] Choose home assembly / package for shared core
- [ ] Extract and compose from BaseApiService + TestApiService
- [ ] SPA + integration tests green
- [ ] `dev build` 0/0

## Notes

Parent: F-015. SPA side already aligned with TestApiService catch filter (131 interim).

## Session

- Created: 2026-07-28 — from task 131 disposition
