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

## Verification concerns to honor (claude-verification, 131 review audits)

Fold these into the design — they are constraints and known traps, not suggestions:

1. **Home must be WASM-safe.** The shared core is composed by the SPA (browser WASM) and
   by test hosts — it must live in a client-safe foundation layer (where
   `IApiService`/`IApiRequest`/`ContractSerializationDefaults` already sit), NOT
   foundation-server. No ASP.NET server dependencies may leak into the SPA download.
2. **Token acquisition is an async per-request seam, not a constructor value.** SPA side
   acquires via `IAccessTokenProvider` (async `SetBearerTokenAsync` before each send);
   TestApiService pins a fixed header at construction. The delegate the core exposes must
   support async per-request acquisition or the SPA side cannot compose it.
3. **Verbatim extraction, one decided exception.** Copy transport semantics byte-for-byte
   (131-002 rule: no drive-by "improvements") — including the 204→`SharedProblemDetails`
   "No Content" mapping and the 499 cancellation mapping. The one deliberate change:
   the verb matrix. Decide Head/Options ONCE, coherently with F-008's fail-closed server
   posture — either support them for real or `NotSupportedException` in dispatcher AND
   remove the dead Head/Options arms in `PrepareContent`/`PrepareRoute` (round-1 found the
   SPA dispatcher throwing while its own `PrepareContent` handled them).
4. **Reconcile the Stream/FileResponse success path.** TestApiService special-cases
   `typeof(TResponse) == typeof(Stream)` → `FileResponse`; verify BaseApiService parity and
   extract one behavior — a silent difference here is exactly the mirror-drift class this
   task exists to kill.
5. **Design regions move with the code** (agent-context-regions rule): TestApiService's
   "mirrors BaseApiService" Design region must be rewritten to describe composition; the
   security/ordering rationale for problem mapping lives once, on the core.
6. **Adjacent one-liner rider (from 131-001 audit):** `EndpointEmitModel`'s Design comment
   claims "Equatable (record + ImmutableArray)" — `ImmutableArray<T>` equality is
   reference-based, so record equality never holds when Tags differ by instance. Harmless
   (pipeline roots at CompilationProvider), but fix the comment to be honest (or add a
   structural comparer if cache-hits ever matter). One line in
   `source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs`.

## Checklist

- [ ] Choose home assembly / package for shared core (WASM-safe client layer — concern 1)
- [ ] Extract and compose from BaseApiService + TestApiService (verbatim rule — concern 3)
- [ ] Async per-request token seam works for SPA `IAccessTokenProvider` (concern 2)
- [ ] Head/Options decided once; dead arms removed or verbs supported (concern 3)
- [ ] Stream/FileResponse path reconciled to one behavior (concern 4)
- [ ] Design regions rewritten on both composers + core (concern 5)
- [ ] `EndpointEmitModel` equatability comment fixed (concern 6 rider)
- [ ] SPA + integration tests green
- [ ] `dev build` 0/0

## Notes

Parent: F-015. SPA side already aligned with TestApiService catch filter (131 interim).
Verification concerns above sourced from
`kanban/in-progress/131-complete-repo-code-review-by-kimi-k3/review/round-1/claude-verification.md`
(§ F-015) and the 131-001 post-done audit (EndpointEmitModel comment nit).

## Session

- Created: 2026-07-28 — from task 131 disposition
