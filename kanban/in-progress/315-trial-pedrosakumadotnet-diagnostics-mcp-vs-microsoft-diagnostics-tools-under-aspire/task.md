# Trial pedrosakuma/dotnet-diagnostics MCP vs Microsoft diagnostics tools under Aspire

## Description

Time-boxed trial of the pedrosakuma/dotnet-diagnostics MCP server/CLI against Microsoft's own diagnostics CLIs
(dotnet-counters, dotnet-trace, dotnet-gcdump) for agent-driven performance investigation of Architecture running
under Aspire. Busara rated the trial GREEN and taking it as a product dependency KILL. We have never used the
Microsoft tools before, so installing them and teaching agents about them is part of the value either way.
Background brief (Akili): 2026-10-12 dotnet-diagnostics intel evaluation (no local file required).

## Requirements

1. **Install (dev-only, pinned).** Never as NuGet references in product code.
   - Add `dotnet-counters`, `dotnet-trace`, `dotnet-gcdump` to the local tool manifest `.config/dotnet-tools.json` with pinned versions.
   - Install the pedrosakuma/dotnet-diagnostics MCP server/CLI at a pinned version (tool manifest if it ships as a dotnet tool, otherwise a documented pinned install).
   - Check and record the licenses of all four.
2. **Make them known to agents.**
   - Register the MCP server in the repo's `.mcp.json`, alongside the Aspire MCP.
   - Add a short skill (or a brief AGENTS.md entry) saying when and how to use each tool. Keep it brief, per
     Flow task 135 (AGENTS.md hygiene); prefer a skill over AGENTS.md body text.
3. **Trial (time-boxed).** Run Architecture under Aspire, then have an agent answer one real question (e.g. where
   CPU or allocations go on a hot endpoint) first with the MCP, then with the Microsoft CLIs. Compare speed and
   clarity; record both in this task.
4. **Decide.** Keep the MCP only if clearly better and faster, with a quarterly maintainer-health recheck.
   Otherwise remove it and keep the Microsoft tools plus the skill.

## Goal (implementer restatement)

Give agents working in this repo (and in generated apps, since `.config/`, `.mcp.json`, and
`skills/` ship in the template) a dev-only way to answer "where does CPU / memory go" in a running
service, and find out whether the third-party MCP earns a place next to Microsoft's own CLIs.

Acceptance:

- `dotnet-counters`, `dotnet-trace`, `dotnet-gcdump` pinned in `.config/dotnet-tools.json`; no
  NuGet reference in product code.
- `dotnet-diagnostics-mcp` pinned for the trial; all four licenses checked and written down here.
- The MCP is registered next to the Aspire MCP for the trial.
- A short skill says when and how to use each tool; AGENTS.md gets at most one table row pointing
  at it.
- One real question is answered twice against a live Architecture service, once through the MCP
  and once through the CLIs, with time and output size recorded here.
- The keep/remove rule is applied: keep the MCP only if it is clearly better **and** faster;
  otherwise remove it from the manifest and `.mcp.json` and keep the CLIs plus the skill.
- `dev build` green, `dev lint-skills` green, `ganda repo audit` green.

Fix pass (review round 1, M1-M3): only `skills/**` ships in a generated app, so the skill itself
must carry the three manifest entries and say a generated app adds them; the template smoke must
require `tw-dotnet-diagnostics/SKILL.md`. The counters section must describe what the pinned
exporter writes (per-interval `Rate` deltas of cumulative counters, CPU split by `cpu.mode`) and
how to turn it into cores, MB/s, and pause fraction. Every example writes under `/tmp`.

## Out of scope (until the trial passes)

- BenchmarkDotNet diagnoser
- Nuru commands
- State dev tools integration

## Checklist

- [x] Tools pinned in `.config/dotnet-tools.json`; MCP pinned; licenses recorded
- [x] MCP registered in `.mcp.json` next to Aspire MCP (trial commit a4ffa32c7)
- [x] Skill / brief AGENTS.md entry added
- [x] Trial run and results recorded
- [x] Keep/remove decision made and applied (removed)

## Licenses

| Tool | Version | License |
|------|---------|---------|
| `dotnet-counters` | 10.0.750501 | MIT (nuspec license expression; dotnet/diagnostics) |
| `dotnet-trace` | 10.0.750501 | MIT |
| `dotnet-gcdump` | 10.0.750501 | MIT |
| `dotnet-diagnostics-mcp` (pedrosakuma) | 0.34.2 | MIT (nuspec and GitHub repo license) |

## Trial (2026-11-25)

**Setup.** `api-server` Release build, `ASPNETCORE_ENVIRONMENT=Development`, run on its own with
`dotnet bin/Release/net11.0/api-server.dll` on `127.0.0.1:5315`. Deviation from "under Aspire":
the AppHost was not started, because every task worktree shares the master AppHost user-secrets
id and a headless AppHost run has rewritten the postgres password before. An Aspire project
resource is the same kind of process; both tool sets attach by pid. Load: 4 keep-alive clients on
`GET /api/weatherforecast?days=50`, about 5,550 requests/s. The MCP was driven over stdio
(`dotnet dotnet-diagnostics-mcp --stdio`) by a small JSON-RPC client, the same calls an agent
makes, so wall time and reply size are measured.

**Question.** Where do CPU and allocations go on the weather-forecast endpoint under load?

**MCP (`dotnet-diagnostics-mcp` 0.34.2).**

| Call | Wall | Reply |
|------|------|-------|
| start + `initialize` | 1.0 s | |
| `tools/list` (13 tools) | 0.1 s | 268,899 bytes |
| `inspect_process view=triage` (5 s window) | 6.7 s | 13 KB |
| `collect_sample kind=cpu` (10 s) | 13.6-16.8 s | 13 KB; summary shows top 3 frames, all thread-start and wait frames |
| `query_snapshot view=top-methods rankBy=exclusive` | <0.1 s | 29 KB |
| `collect_sample kind=allocation` (10 s) | 10.8-11.2 s | 38-65 KB; types by bytes and count |
| `query_snapshot` allocation `top-methods` | <0.1 s | 23 KB; full generic method names |

Friction: `query_snapshot latestOfKind=cpu` returned `NotFound` right after a cpu capture in the
same server process, so the explicit handle had to be passed; the allocation `call-tree` at
depth 8 stopped at `HostingApplication.ProcessRequestAsync` before any application frame; every
reply carries a ~2 KB safety block; the triage called a 2.3-core synthetic load "critical".

**Microsoft CLIs (10.0.750501).**

| Step | Wall | Output |
|------|------|--------|
| `dotnet-counters collect --duration 00:00:10 --format json` | 11.0 s | 69 KB JSON (needs a short parse for last values) |
| `dotnet-trace collect --profile dotnet-sampled-thread-time` (10 s) | 11.0 s | 11 MB nettrace |
| `dotnet-trace report topN -n 25` | 3.9 s | 3 KB table; long generic names cut (`__Canon]].MoveNext()`), `-v` does not restore them |
| `dotnet-trace collect --profile gc-verbose` (8 s) + `report topN` | 12.4 s | 3 KB; allocating call sites by method, no types |
| `dotnet-trace convert --format speedscope` | ~3 s | full frame names (`FluentValidationBehavior...`) |
| `dotnet-gcdump collect` + `report` | 4.6 s + 2.4 s | 537 KB report, head -40 is the useful part; live heap 17 MB |

**Answer (both tool sets agree).** About 2.3 cores, 68-96 MB/s allocated, gen0 only, GC pause
about 1.4 % of time, 90-116 monitor contentions/s, thread-pool queue 0. Sampled CPU stacks are
mostly idle thread-pool and socket waits; application-side frames are small:
`AuthenticationServiceImpl.AuthenticateAsync` (~1 % inclusive), the DI scope's
`Dictionary<ServiceCacheKey,…>.Resize`, `DebugLogger.Log` (Development debug logging),
`HostingApplication.CreateContext`. Allocations after the response payload itself
(`WeatherForecast` objects, arrays, strings) come from per-request FluentValidation rule building
(`FluentValidationBehavior.Handle` -> `AbstractValidator.RuleFor` -> `PropertyRule.Create`,
`PropertyChain.FromExpression`, `SplitPascalCase`), DI scope dictionary growth, tracing
(`Activity`, `TagList`, `DiagNode`), and logger scopes (`ExtendedLogger.BeginScope`).

**Comparison.**

- Speed: the same. Both are bounded by the capture window (about 45 s each for the full answer).
  For CPU the MCP uses the same EventPipe sample profiler as `dotnet-trace`, so it adds no new
  signal; the hot-method lists match row for row.
- Clarity: the MCP is clearer for allocations (types and call sites in one place, full names)
  and its next-step hints are useful. The CLIs reach the same answer with `gc-verbose` + `topN` +
  speedscope conversion, at 3 KB per table instead of 13-65 KB per reply.
- Cost: registering the MCP adds a 269 KB tool catalog (about 67k tokens) to every session in a
  harness that loads all MCP tools (OpenCode, Grok), whether or not the session profiles anything.
- Health: pre-1.0, one maintainer, 27 stars, releases every few weeks (0.33.0 -> 0.34.2), fields
  already deprecated for removal at 1.0, and a wide surface (HTTP bearer/OIDC, SQLite captures,
  ptrace, dumps, dynamic method instrumentation).

## Decision (2026-11-25)

Remove the MCP. The rule was "keep only if clearly better and faster": it is not faster, and it
is better only for allocation views that the CLIs also reach. Applied: `dotnet-diagnostics-mcp`
removed from `.config/dotnet-tools.json` and `.mcp.json` (the trial state is commit a4ffa32c7).
Kept: `dotnet-counters`, `dotnet-trace`, `dotnet-gcdump` with `rollForward: true` (the packages
target net8.0 only, so a machine with only the .NET 11 runtime needs roll-forward), and the skill
`skills/tw-dotnet-diagnostics`. No quarterly recheck is owed, since the MCP is not kept.

## Learnings

- Per-request FluentValidation rule building is the largest application-side allocator on the
  reference endpoint. `api-server/program.cs` registers validators with
  `AddValidatorsFromAssemblyContaining` (scoped by default), so
  `TimeWarp.Foundation.Behaviors.FluentValidationBehavior` gets a new validator per request and its
  constructor runs `RuleFor` each time. A follow-up task could register them as singletons; it
  needs its own benchmark (`tw-perf-sweep`).
- `dotnet-trace report topN` on a `gc-verbose` trace ranks allocating call sites; Microsoft's docs
  do not say so.
- Misread (review round 1): the implementer restatement says generated apps get this because
  `.config/`, `.mcp.json`, and `skills/` all ship in the template. Only `skills/**` is in the
  pack allow-list (`timewarp-architecture-template.csproj`). `.config/dotnet-tools.json` and
  `.mcp.json` are not packed, and `AGENTS.md` is not packed. The same restatement's acceptance
  says the question is answered against "a live Architecture service". Requirement 3 says to run
  Architecture under Aspire. The trial used a standalone `api-server` and recorded why (shared
  AppHost user-secrets). That deviation is not a separate finding: both tool sets attach by pid,
  and the keep/remove rule does not depend on the host.

## Results

Files: `.config/dotnet-tools.json` (three CLIs pinned), `skills/tw-dotnet-diagnostics/SKILL.md`
(new), `AGENTS.md` (one "Where to look" row),
`tools/dev-cli/services/template-smoke/template-smoke-generated-content.cs` (smoke requires the
new skill; count printed from the list). `.mcp.json` ends unchanged from master.

Fix pass, review round 1:

- M1 fixed: the skill says the pins live in this repository's manifest, shows the three entries
  (10.0.750501, `rollForward: true`) for a generated app to add, and says why roll-forward is
  needed. `AssertSkillsShipped` requires `tw-dotnet-diagnostics/SKILL.md`; the "ten" wording is
  gone (the message prints the list length, 11). `.config/` stays unpacked.
- M2 fixed: checked against the trial's 10.0.750501 JSON (`/tmp/t315/ms/counters.json`). The
  runtime counters are cumulative; the exporter writes each as a per-refresh-interval delta,
  `counterType: "Rate"`, name suffix `(s / 1 sec)` / `(By / 1 sec)`, value in `value`; CPU comes as
  `cpu.mode=user` and `cpu.mode=system` series. The skill now says that, gives a parse that
  averages the `Rate` events (it printed 1.34 + 0.65 = 2.0 cores, 85 MB/s, pause 0.0116 on the
  trial file), and derives CPU share and pause fraction.
- M3 fixed: every example writes under `/tmp`; the rule names counters files and the 11 MB size.

### How to validate

Smoke:

```bash
dotnet tool restore
dotnet dotnet-counters --version && dotnet dotnet-trace --version && dotnet dotnet-gcdump --version
# with any service running under load:
dotnet dotnet-trace ps | grep api-server
dotnet dotnet-trace collect -p <pid> --profile gc-verbose --duration 00:00:00:10 -o /tmp/alloc.nettrace
dotnet dotnet-trace report /tmp/alloc.nettrace topN -n 25
```

Expect: restore succeeds; each tool prints `10.0.750501+…`; `report` prints a "Top 25 Functions"
table; `.mcp.json` lists only the `aspire` server.

### Gates

Fix pass (review round 1):

- `dotnet run --file tools/dev-cli/dev.cs -- self-install` (dev-cli source changed), then
  `./bin/dev build`: pass (28.3 s).
- Test files: none added or edited. No Jaribu test covers `TemplateSmokeGeneratedContent`; the
  template smoke is its check (next line).
- `./bin/dev template-smoke`: pass; "Generated app contains skills/ (11 SKILL.md files; analysis/
  excluded)", generated app Jaribu suites green, "Template smoke SUCCEEDED".
- `./bin/dev lint-skills`: pass.
- `ganda repo audit`: fail on `kanban-landed` only (task 522, another live walk, kitchen not
  landed on origin-home). `ganda repo audit --fix` landed it (`499db4623 land kanban 522` on
  origin-home); result: all checks pass.

Implement pass:

- `./bin/dev build`: pass, 0 warnings, 0 errors (25.7 s).
- Test files: none added or edited. No C# changed (tool manifest, a skill, and AGENTS.md only), so
  no test file covers the change. CI runs the full suite and the template smoke on the PR.
- `./bin/dev lint-skills` (skills/ changed): pass, 11 skills including `tw-dotnet-diagnostics`.
- `ganda repo audit`: pass with one advisory (`vscode-window-icon`, `.vscode/settings.json`
  missing). `ganda repo audit --fix` wrote that git-ignored local file; rerun: all checks pass.

## Session

- Created: grok-bot (2026-10-12)
- 2026-11-25 implementer (claude, ganda task work): pinned tools, ran the trial on standalone
  api-server, removed the MCP per the rule, added `tw-dotnet-diagnostics`.
- 2026-11-25 implementer (claude, ganda task work, implement-fix-2): fixed review round 1 M1-M3
  (skill manifest entries for generated apps, smoke list, counters JSON parse, `/tmp` paths).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session da67d941-9e83-4098-ae56-cac8b39ecc25, max-turns 500, Done — 2026-11-24T20:52:57Z
- Review round 1: grok, session 01a20f55-6ee6-7e13-896f-e20bdede3a55 (2026-11-25). Effort 2, roster general. Open findings M1, M2, M3. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20f55-6ee6-7e13-896f-e20bdede3a55, max-turns 120, artifacts general.md, merged.md — 2026-11-24T21:02:34Z

## Review

- Round 1: Blocked — open M1, M2, M3
