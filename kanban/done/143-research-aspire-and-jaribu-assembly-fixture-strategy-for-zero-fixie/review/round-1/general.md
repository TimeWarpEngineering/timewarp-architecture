# Round 1 — general
**Date:** 2026-07-31
**Scope reviewed:** `findings.md` + `research/{lifetime-inventory,aspire-fit,jaribu-fixture-options}.md`

## Summary

High-quality research synthesis. The headline claim — that TimeWarp.Fixie never delivered
assembly-scoped host lifetime — is independently verified by decompiling TimeWarp.Fixie 3.1.0
`TestExecution.Run` / `CreateClassServiceProvider`. That correctly reframes zero-Fixie: Jaribu
class-scoped `SetupOnce`/`CleanUpOnce` is lifetime-equivalent to real Fixie behavior for hosts.
Three-framework inventory (Fixie / Jaribu / xUnit on aspire-tests), two-lane Aspire model, and
the B→D collapse for run-scope hooks are sound. Preferred C+A + data-gated E is directionally
right. Main gap: **C is underspecified** (create-per-class vs process-static share), so §5
“no structural blockers” is slightly oversold until dispose semantics for C under JARIBU_MULTI
are explicit. Recommend fold-in before Steve locks §6.

## Verification results

| # | Claim | How checked | Result |
|---|--------|-------------|--------|
| 1 | Fixie builds SP per test class, disposes between classes | Decompile `~/.nuget/packages/timewarp.fixie/3.1.0/lib/net9.0/TimeWarp.Fixie.dll` → `TimeWarp.Fixie.TestExecution.Run` / `CreateClassServiceProvider` | **CONFIRMED** — foreach class: new ServiceCollection, re-run ConfigureAdditionalServices, BuildServiceProvider, DisposeAsync after class |
| 2 | `.AddSingleton<WebTestServerApplication>()` is singleton-per-class-provider | Same decompile + `testing-convention.cs` AddSingleton registrations | **CONFIRMED** |
| 3 | aspire-tests is xUnit (third framework) | `aspire-tests.csproj` PackageReference xunit; `IngressSmokeTests : IClassFixture<>` + `[Fact]` | **CONFIRMED** |
| 4 | AGENTS.md says host-level Fixie, silent on xUnit | AGENTS.md Stack tests bullet | **CONFIRMED** drift |
| 5 | MOCK_AUTHENTICATION is compile-time `#if` | `source/.../web-spa/program.cs` + mock provider files | **CONFIRMED** |
| 6 | B unreachable under `dev test` without Jaribu change | Stream 3 + known MTP ExecuteRequestAsync loop (task 136) | **AGREE** (B collapses to D) |
| 7 | E = empty MTP session hooks | Cited jaribu-test-framework CreateTestSessionAsync/CloseTestSessionAsync | **PLAUSIBLE** — not re-decompiled this round; accept stream 3 unless contradicted |
| 8 | web BFF ~14 host boots / ~24 files | `rg -l WebTestServerApplication` web-server-integration-tests ≈ 24 files | **PLAUSIBLE** (~24 files; boots ≈ consuming classes) |
| 9 | C “idempotent” + “per-class boot parity” | Text-only | **TENSION** — see Issue 1 |

Decompile excerpt (evidence for claim 1):

```csharp
// TimeWarp.Fixie.TestExecution.Run (3.1.0)
foreach (TestClass testClass in testSuite.TestClasses)
{
  ServiceProvider classServiceProvider = CreateClassServiceProvider(testClass.Type);
  // per-method IServiceScope; hosts are Singleton in class provider
  await classServiceProvider.DisposeAsync();
}
```

## Issues

### Issue 1 — Severity: suggestion
- File: `findings.md` §3, §5
- Description: Preferred model C is described as an “idempotent async factory” **and** as
  delivering “cost parity with Fixie (per-class boots).” Those imply opposite lifetimes:
  **C-create** (factory always builds a new graph → N boots, simple CleanUpOnce dispose) vs
  **C-share** (process-static / refcounted graph → fewer boots, cannot dispose on first class’s
  CleanUpOnce without refcount or run-scope teardown). Stream 3 already flags “C alone
  reintroduces undisposed-static” and “refcounting risk”; findings soft-merge into C+A without
  picking create vs share. Until explicit, §5 “None structural” oversells closure.
- Suggestion: Amend §3 to default **C-create** for day-one zero-Fixie (parity with measured
  Fixie; mandatory CleanUpOnce disposes that class’s graph). Treat **C-share** + E (or
  refcount) only after measured aggregator wall-clock — same bar as §7 item 6. Soften §5 to
  “no structural unknowns once C dispose contract is chosen.”
- Status: open

### Issue 2 — Severity: suggestion
- File: `findings.md` §6–§7
- Description: Findings assume migrating **existing Fixie project topology** (e.g.
  web-server-integration-tests multi-class suite) onto C+A. Research proves 1:1 port is
  *feasible*; it does not choose between **(α)** same suite shape, Jaribu runner vs **(β)**
  co-located product default + suite shrink (tasks 134–136 model). Hybrid is valid (aspire-tests
  stay suite-shaped; product endpoints co-locate).
- Suggestion: Add one decision bullet under §6: migration topology α / β / hybrid; default
  hybrid recommended (co-locate product endpoints; keep topology suites suite-shaped).
- Status: open

### Issue 3 — Severity: suggestion
- File: `findings.md` §3; `research/lifetime-inventory.md` foundation Testcontainers note
- Description: Inventory notes process-static Lazy for postgres in foundation/infra tests.
  Findings do not fold that into C — either C is the **one** allowed process-static lifetime
  home (with explicit dispose rules) or infra remains a third pattern.
- Suggestion: One sentence in §3 or §8: Testcontainers Lazy either migrates under C’s rules or
  stays documented exception.
- Status: open

### Issue 4 — Severity: nit
- File: `findings.md` §1
- Description: Headline is correct but reads as opinion without package/type citation. Reviewer
  re-verified via decompile of TimeWarp.Fixie **3.1.0** `TestExecution.Run`.
- Suggestion: Cite package version + type/method name (and/or link stream 1) so the correction
  survives session handoff.
- Status: open

### Issue 5 — Severity: nit
- File: `findings.md` §1, §7 item 3
- Description: “~14×” boots vs “~24 files” — consistent with approximate consumer counts but
  easy to over-read as exact.
- Suggestion: Point at `research/lifetime-inventory.md` consumer table; keep approximate language.
- Status: open

### Issue 6 — Severity: suggestion (advisory, not findings defect)
- File: n/a (endorsement of §6 with amendment)
- Description: Draft recommendation is endorsable once Issue 1 is resolved.
- Suggestion: North star = single-framework Jaribu (zero Fixie **and** zero xUnit); lifetime =
  C+A with **C-create**; Aspire two-lane as written; C-share/E optimization only after data;
  single-file co-located remains default authoring for product slices.
- Status: open

## Non-issues (checked, not raised)

- Aspire DI process wall — still true; not re-litigated.
- B≡D under dev test — correct.
- E data-gated — correct; standalone will not get E (acceptable).
- Wall-clock **measurements** in research streams (20–30s boots, suite timings) are empirical
  data, not forbidden calendar estimates of future work — fine under AGENTS.md ban.
- Scrutor vs ModuleInitializer — real ergonomic gap, not a structural blocker.
