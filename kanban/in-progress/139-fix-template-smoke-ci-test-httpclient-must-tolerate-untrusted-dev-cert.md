# Fix template-smoke CI: test HttpClient must tolerate untrusted dev cert

## Description

PR #293's template-smoke CI job failed on the new tier-2 check: the generated co-located
integration runfile (`get-weather-forecasts-tests.cs`) spins Kestrel on
`https://localhost:7255`, and GitHub runners have no trusted ASP.NET dev certificate — both
tests failed with `HttpRequestException: The SSL connection could not be established`
(Kestrel logged the untrusted-dev-cert warning). Passed locally only because WSL has the dev
cert trusted (see how-to-trust-aspnet-dev-certificate-when-using-wsl.md). Never surfaced
before because CI has no test gate — tier 2 is the first HTTPS integration test to execute
on a runner.

Fix: `TimeWarp.Architecture.Testing.TestServerApplication<TProgram>` now builds its
`HttpClient` with a handler whose `ServerCertificateCustomValidationCallback` accepts
certificates **only for loopback hosts** (non-loopback endpoints still validate normally).
This also makes the whole integration tier CI-runnable, which task 136 (aggregators in
`dev test`) needs anyway.

## Checklist

- [x] Loopback-scoped cert callback in `tests/common/timewarp-testing/test-server-application.cs`
      (commit f3c2bee6)
- [x] `dev build` 0/0
- [x] Integration runfile passes locally (2/2)
- [ ] Pushed to dev; template-smoke CI green on PR #293

## Results

Commit f3c2bee6 on dev. One-handler fix in shared test infra, scoped to loopback so real
endpoint validation is unchanged. Local gates green; awaiting CI confirmation on PR #293.

## Session

- Created + fixed: c6f1a13b-487f-4085-bf61-ba4761e8579e (2026-07-30)
