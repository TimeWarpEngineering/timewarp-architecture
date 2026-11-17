# Round 3 — general
**Date:** 2026-11-05
**Scope reviewed:** fix delta `29d388aff..dfcd31272` (fix pass 2 for M5). Kitchen only: Option D
and the B `SetRolePermissions` example in `proposal.md`, plus `task.md` Session and Results. No
product paths changed.

## Summary

Fix pass 2 rewrites Option D so a `ContractResult<T>` slot cannot stay at `Running`. `Running`
moves from `GetRequest` to a new `OnStarted(TRequest)` virtual, which is called after the null
skip. The existing `catch (Exception)` calls `OnProblem` with a synthetic problem before `throw;`.
A table lists every path out of `ApiHandler.Handle`. Two rejected remedies and two accepted
coding-bug gaps are named. I re-checked the claims against the tree and they hold. M5 is fixed,
and I found no new defects in the delta.

## Verification (re-run against the tree)

- `api-handler.cs:70-71`: `GetRequest`, then `if (request is null) return;`. The proposed
  `OnStarted(request)` slots in after `:71`, inside the `using` lock and before
  `ValidateRequestAsync` (`:73`). That matches the proposed comment.
- `api-handler.cs:77-86`: the `OperationCanceledException` catch maps to a synthetic 499, which
  goes through the problem arm. `HttpApiService` (`foundation-contracts/services/http-api-service.cs:89`)
  also maps OCE to 499. Its only other catch (`:202`) is a filtered `JsonException` /
  `InvalidOperationException` during problem-body deserialization. So an `HttpRequestException`
  from `HttpClient` reaches the rethrow at `api-handler.cs:87-92`, as the table and the Session
  note say.
- `api-handler.cs:123`: `ValidationException` is thrown inside the `try` and reaches the same
  catch. The proposal keeps the throw, which preserves the Design-region rule at `:20-22`.
- Lock release: the `using SemaphoreGuard` declaration is scoped to the `try` block. Its
  `Dispose` (a nested finally) runs before the outer `catch` body, because .NET runs inner
  finally blocks before the handler. So "`OnProblem` never runs under the lock from that path"
  is correct.
- `api-handler.cs:94-97`: `HandleApiResponseAsync` is outside the `try`. So the two accepted gaps
  are real: `DefaultApiHandler.HandleFileResponse` throws `NotImplementedException`
  (`default-api-handler.cs:32-34`), and a hand-written `HandleSuccess` can throw before its
  write. Neither reaches the catch.
- The "`GetRequest` itself throws" row is consistent. The catch fires with `OnStarted` not yet
  run, and `OnProblem` sets `Failed`.
- Coherence: line 270's flag-reset in the B example was removed and replaced by a generated
  `OnStarted` override. The "Who sets the slot" bullets, the A-only check (which now also flags
  `Running` in `GetRequest`), and the scope line all name the same two virtuals and three call
  sites. The proposed `HandleApiResponseAsync` problem arm calls `OnProblem` before
  `HandleError`. The `task.md` Results text for Option D matches.
- Goal restatement vs. Description: the delta does not touch either, and they still match. There
  is no misread.

## Issues

None.
