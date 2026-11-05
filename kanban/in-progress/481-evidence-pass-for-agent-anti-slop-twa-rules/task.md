# Evidence pass for agent anti-slop TWA rules

## Description

Research only. Mine recent agent PRs and Learnings for C# patterns agents tend to produce that appear ≥2 times across TimeWarp repos. Deliver a ranked list of candidate patterns with evidence (repo, PR/task, count). No analyzer code in this task.

After the evidence pass, each pattern that still stands can become its own TWA analyzer implement task, one rule at a time in one repo. Do not ship a speculative pack.

## Requirements

- Search recent agent PRs and kanban `## Learnings` sections for repeated C# anti-patterns.
- Rank by frequency (≥2 occurrences required to stay on the list).
- Cite concrete evidence for each row (repo, PR or task id, short description).
- Output is a ranked list only — no Roslyn / TWA analyzer code, no package scaffold.

## Goal (implementer restatement)

Find C# mistakes that coding agents make again and again in TimeWarp repos, using real
evidence rather than guesses. The output is a ranked table, not code.

Acceptance criteria:

- The corpus is written down: which repos, which time window, and which kanban files were
  read (implementation-review findings and `## Learnings` sections).
- Each pattern on the list has at least 2 cited hits. A citation is repo, task or PR id, and
  a one-line description. Patterns with 1 hit are dropped or listed separately as "watch".
- Patterns that an existing TWA / TWE / TW rule already enforces are marked as covered, so
  the list shows only new rule candidates.
- The list is ranked by hit count, and each row says whether a Roslyn analyzer can detect it.
- Follow-on implement tasks (one rule each) are proposed in Results and are **not** filed.
- No analyzer code, no package scaffold, no product source changes.

## Checklist

- [x] Define the search corpus (which repos, which PR window, where Learnings live)
- [x] Collect candidate patterns with ≥2 hits
- [x] Rank and write up the evidence list in Results
- [x] Propose follow-on implement tasks (one rule each) — do not file them until Steven confirms

## Session

- Created: 874084 (2026-11-05)
- 2026-11-06 implement oracle (`ganda task work`, headless). Four read-only sonnet sub-agents
  each swept one slice of the corpus (architecture; ganda; nuru and taratibu; state, mediator,
  amuru, jaribu, and terminal). I re-checked 25 of their citations by grepping the cited task
  folders. 24 matched as written. One (ganda 493) matched under different wording, and the row
  below quotes the real text. No product source changed.
- Gates: the first `ganda repo audit` failed on `kanban-landed`, because task 464's kitchen
  differed from origin-home. That was another session's claim, and 464 landed on origin-home
  while this walk ran. On re-run the audit passes with 2 advisory warnings: `kanban-landed`
  NoUsableWorktree warnings, and `source-file-line-cap` on EF migration designers and a 238
  research runfile, which is warning severity since 469-001. `audit --fix` changed nothing.
  No build or test ran, since this is a kanban-only research change.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-sonnet-5-5), session 366929c4-70ea-405d-aa73-163352c48be3, max-turns 500, Done — 2026-11-05T18:47:26Z
- 2026-11-06 review oracle round 1 (claude-opus-5-5, headless, effort 2, roster general,
  turn cap 120). Re-ran the validate smoke and spot-checked 10 more citations plus the
  rule-coverage claims; all held. Findings: M1 bug (table not in hit-count order), M2 nit
  (row 12 count vs split). 2 open, so no disposition yet; fix pass requested. Trail:
  `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session b84b50e6-4fcc-4ddc-b501-7cb451f81d2b, max-turns 120, artifacts general.md, merged.md — 2026-11-05T18:49:38Z
- 2026-11-06 implement-fix-2 (round 1 fix pass, headless). M1: reordered the ranked table
  strictly by hit count (old rows 6↔7, 10↔11, 14↔15, 16↔17 swapped; ties keep sweep order) and
  renumbered every row reference in the rule-pack paragraph, the follow-on proposals, and How to
  validate. M2: row 12 headline is now `~11`, matching its repo split. I re-summed every other
  row's split against its headline; all agree. Kanban-only change, so no build or test ran.

## Review

- Round 1: Blocked — open M1, M2

## Notes

- Source: X web-rewrite agents eval option A (REWORK). https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-05-x-web-rewrite-agents.md (PR https://github.com/TimeWarpEngineering/timewarp-intel/pull/103). Busara: mine real agent PRs / Learnings first; ship one TWA rule at a time. Confirmed by Steven 2026-11-05.
- Analyzers live under `source/analyzers/` in this repo when implement tasks are filed later.
- Filed only; not launched. Do not start until Steven says so.

## Results

### Corpus

- **Repos:** timewarp-architecture, timewarp-ganda, timewarp-nuru, timewarp-taratibu,
  timewarp-state, timewarp-mediator, timewarp-amuru, timewarp-jaribu, timewarp-terminal. These
  are the C# repos that have agent-written kanban review trails. Each was read at its
  origin-home `master` checkout on 2026-11-06.
- **Window:** review files committed from 2026-09-01 to 2026-11-06. The file list came from
  `git log --since=2026-09-01 --name-only -- kanban | grep -E 'review/.*(merged|disposition)\.md'`,
  with each path mapped to the task folder's current column.
- **Where the evidence lives:** implementation-review findings in
  `kanban/*/<task>/review/round-N/merged.md`. That is where every agent PR's review lands under
  `ganda task work`. Also every `## Learnings` section in a `task.md`.
- **Size:** about 1,500 `merged.md` files with findings, across roughly 700 tasks, plus 108
  `## Learnings` sections. The Learnings added little. Almost all of them are restatement
  ("misread") notes about process, and only about 8 carry a code-level point.
- **Method and its limits:** sub-agents used keyword sweeps plus reading the matched findings.
  They did not read every finding in full, so the hit counts are a **floor**, and counts marked
  `~` are hand counts. A task with several findings of one pattern counts as one hit.
- **Left out:** process findings (kanban, git, PR text), docs and skill prose, CI and shell
  scripts, and one-off feature logic bugs. The one-off logic bugs are roughly 70% of all C#
  findings.
- **Not mined:** GitHub PR comment threads. Under `ganda task work` the review is the kanban
  `review/` trail, so PR threads are mostly empty.

### Not found: the "classic" agent slop

The sweep found **zero or one** hit for each of these: `async void`, `.Result`, `.Wait()` or
`GetAwaiter().GetResult()`, `throw ex`, `Process.Start` and `ProcessStartInfo` (already in
`BannedSymbols.txt`), `System.Console` (banned), `DateTime.Now` (1 hit, taratibu 049
`UtcNow`), culture-sensitive parsing (1 hit, nuru 470-complete), underscore field names, and
files over 500 lines as a standalone finding (TWA0038 and the ganda line cap already catch
those).

The likely reason is that `AnalysisMode=All`, Roslynator, BannedApiAnalyzers and
warnings-as-errors already fail the build on these shapes. What reaches review is the slop
those tools **cannot** see, or the places where an agent **silenced** them.

### Ranked patterns (≥2 hits)

Hits are distinct tasks, and rows run from most hits to fewest; ties keep their sweep order. "Detect" says whether a Roslyn rule can find the pattern. The rule
column names an existing rule, or `none`.

| # | Pattern | Hits (repo split) | Evidence (sample) | Existing rule | Detect |
|---|---------|-------------------|-------------------|---------------|--------|
| 1 | Comment, XML doc, or Design/Purpose region left stale or false after the code changed | ~68 (arch ~32, ganda 14, state 6, mediator 4, amuru 3, nuru 5, taratibu 3, terminal 1) | arch 254 `program.cs:195`: Design says "resolved once at registration", which is false. arch 210-005: Design cites a deleted how-to page. ganda 479 `workflow-change-filter.cs:9`: Design describes the old filter. ganda 551-006: `PrBaseBranch` is dead and its doc is stale. state 139: Design names deleted test files. nuru 482-complete: XML examples call the removed `CreateBuilder([])`. | none (TWA0004 only checks that the region exists; CS1574 catches a dead `cref`) | **no**, the check is semantic. One slice can be detected: a comment that names a repo path that does not exist (arch 210-005, state 139). |
| 2 | Dead or unused code left behind: unused parameter or member, a member kept alive only by tests, a no-op statement | ~38 (ganda ~20, arch 6, mediator 5, state 2, amuru 1, terminal 1, nuru 2, taratibu 1) | ganda 552-015 `doctor-agents-check.cs:144`: `_ = minimumIgnored;`. ganda 551-003: `_ = kanban;`. ganda 431 `command-usage-behavior.cs:74`: `_ = exception.GetType();` keeps a catch non-empty. state 121: `_ = FlushTask.IsCompleted;`. ganda 438: `FindNewTaskByTimestamp` is kept alive by six test fakes. terminal 023: `MeasureHeight` has no references. | partly: IDE0051, IDE0052 and RCS1163. IDE0060 is **silent** in this repo (`.editorconfig`, task 172). | **partly**. Discard statements that exist only to silence a warning are detectable (5 tasks across 2 repos). A member reached only by tests needs whole-program analysis. |
| 3 | Test can't fail: tautological, assertion-free, constant assertion, silent early `return`, or a base-exception throw assertion | ~31 (arch 7, ganda 9, nuru 4, taratibu 2, state 4, mediator 1, amuru 2, jaribu 2) | taratibu 013 `Scaffold_Should_Pass`: `true.ShouldBeTrue()`. state 139: a test logs to a `NullLogger` and asserts nothing. arch 070-004 `kubernetes-publish-tests.cs:248`: the test accepts any `InvalidOperationException`. amuru 104: a symlink test silently returns when the link fails. ganda 265: `ShouldNotBeOfType<Win32Exception>` after `ThrowAsync<InvalidOperationException>`. jaribu 036-001: the assertion only runs when the value is non-null. | none | **partly**. No assertion, a literal-only assertion, an early `return`, and `Should.Throw<Exception>` are syntactic. A tautology is semantic. |
| 4 | Exception handling: empty or discard-only catch, swallowed fault, or a filter too broad or too narrow | ~25 (ganda 8, state 4, amuru 4, nuru 3, taratibu 2, arch 3, terminal 1) | ganda 551-001 `task-context-reader.cs:157`: empty catch-all under a CA1031 pragma. ganda 551-005 `task-signal-log.cs:81`: an empty catch discards the machine. ganda 389: `catch (Exception)` in a test swallows `ShouldAssertException`. amuru 112: `catch (Exception)` returns false. amuru 044: a no-op try/catch. state 121: a fire-and-forget flush fault is swallowed. | partly: CA1031 is on, and agents suppress it (see row 7) | **partly**. Empty and discard-only catch blocks are detectable. Whether a filter's scope is right is not. |
| 5 | Two values must agree and are kept in step by a hand-typed literal or a comment (a magic string that duplicates a constant, or lists that drift) | ~24 (arch 9, ganda 6, nuru 6, state 1, terminal 2) | ganda 282 `stacked-task-set-compute.cs:97`: `"headless"` instead of `TaskWorkHost.HeadlessName`. arch 245: `TIMEWARP_TEST_PORT_BASE` typed as a second literal. arch 438: route literals next to `TipRoute`, and test stubs that hard-code routes. ganda 346: a turn-limit regex that drifted from `TurnLimitCheckpoint`. nuru 482-006 and 491: the `CiTestExcludes` and `standaloneTests` lists drift. | partly: TWA0039–0043 cover contract and aggregate agreement only | **partly**. "A string literal equals an accessible `const` value" is detectable. Lists kept in step by a comment are not. |
| 6 | A second copy of an existing helper, rule, or host instead of reusing it | ~23 (arch ~17, state 2, mediator 1, amuru 2, jaribu 1) | arch 435 `open-api-public-server.cs:56`: "a third copy of the forwarded public-origin rule". arch 399: a row name hand-copied from `UserXaiApiKeyNames.NameFor`. arch 457: a local copy of the DevCli `RepoConfigService`. jaribu 036-002: two emit helpers duplicate each other. | none | **partly**: verbatim clones only (S4144-style). A restated rule is not detectable. |
| 7 | A warning silenced instead of fixed: `#pragma` with no reason or with file scope, `SuppressMessage` with `"<Pending>"`, `UnconditionalSuppressMessage` that hides a real problem, blanket `NoWarn` | 16 (ganda 8, arch 3, taratibu 3, amuru 2) | arch 210-post-migration `web-spa/global-suppressions.cs:11`: three `Justification = "<Pending>"`. ganda 433 `nuru-check.cs:5`: file-level CA1031 where one catch needed it. ganda 444: a CA1822 pragma hides a method that could be static. ganda 551 `task-machine.cs:24`: `#pragma warning disable SM001, SM010` over the whole definition. taratibu 057: IL2075 suppressed as "AOT pack is post-MVP", and the trimmed host then throws a NullReferenceException. amuru 116 and 119: `NoWarn` for IDE0055 and CA1307, the CA1307 one on a false premise. | partly: IDE0079 flags a redundant suppression | **yes**: an empty or placeholder justification, a `disable` with no `restore`, and `NoWarn` entries in props. A false premise is not detectable. |
| 8 | Mutable shared or ambient state: a static non-readonly field, a public static array, `[ThreadStatic]` across an await, a static field on a generic type | ~14 (arch 4, ganda 1, state 3, mediator 1, amuru 1, jaribu 2, terminal 2) | arch 469-006 `host-graph-lease.cs`: a process-static ref-counted field. arch 342 `api-handler.cs:28`: a static semaphore table on a generic class. ganda 513-001: a public static `ListenersOverride` test hook. mediator 021-003: `[ThreadStatic] Scope` is lost across an await, and the manifest exposes mutable static arrays. terminal 029: process-global `Terminal.FormatProvider`. | partly: CA2211 (non-private only) and CA1000 | **yes** for the shapes. Whether the state is safe is semantic. |
| 9 | Line-cap or width gaming and over-long lines: rows packed on one line, docs deleted to hold the cap, comment lines over the width | ~12 (ganda 4, state 1, arch 7 with width-only nits) | ganda 551-003 `task-machine-definition.cs`: 205 lines over 200 characters. ganda 551-001: four Design lines deleted to stay at the 649-line baseline. ganda 551-004: XML docs deleted to hold the baseline. state 135: why-comments deleted to reach 500 lines. arch 279 and 280: Design lines of 153 and 160 characters. | none enabled. Roslynator **RCS0056** (line too long) is in the already-referenced `Roslynator.Formatting.Analyzers`, but is not configured. | **yes** for long lines. Deleting docs to fit the cap is not detectable. |
| 10 | Branch on text: `Exception.Message` matching, substring or regex scans of structured output, matching a symbol by simple name | ~12 (ganda 7, nuru 3, state 1, mediator 1) | ganda 493: catches `InvalidOperationException` by matching Taratibu's message "RunNestedAsync requires an active parent walk". ganda 389: `Message.Contains("linked worktree")`. ganda 476: unblock wording keyed on a refusal-message substring. nuru 541: `typeName.EndsWith("ILoggerFactory")`. state 107: an attribute matched by simple name. | none | **partly**: `<exception>.Message` used in a comparison or `Contains` is detectable. Stdout scanning is not. |
| 11 | Silent skip in a generator or analyzer: unsupported input is dropped with no diagnostic | ~11 (arch 6, state 3, mediator 2) | arch 281: `[Offerable]` on a non-partial class is silently ignored. arch 300 and 303: a non-literal `NavGroup` or Description silently becomes `""`. state 092: generic types are silently skipped. mediator 021-006: a `[MediatorBehavior]` spelled through a using alias is silently dropped. | none | **no** as a general rule. It is case-by-case per generator (several TWE ids already fix single cases). |
| 12 | Test environment leaks: changes the process CWD or environment variables, uses a shared temp file, leaves temp dirs behind, or uses `Environment.ExitCode` and `Exit` | ~11 (amuru 5, ganda 3, nuru 1, state 2) | amuru 099: a test changes the process CWD. amuru 021: `GetTempFileName` shared root. ganda 358: a test mutates PATH and hard-codes `:`. ganda 519 and 436: temp dirs never deleted. nuru 470-007: `Environment.ExitCode` is asserted without a reset. state 080 and 137: `Environment.Exit` and `ExitCode` used as control flow. | none | **yes**, with banned symbols in test projects. |
| 13 | Async work not cancelled or not guarded: `CancellationToken.None` passed while a token is in scope, a component await that outlives Dispose | ~11 (arch 5, ganda 2, state 2, amuru 1, jaribu 1) | ganda 452 `campaign-compute.cs:208,213,503`: nested walks get `CancellationToken.None`. arch 239 `CommandPalette.razor:13`: keydown registration completes after Dispose. arch 327: two fast clicks start two un-cancelled fetches. | partly: CA2016 (an omitted token, not an explicit `None`) | **partly**: explicit `CancellationToken.None` with a token parameter in scope is detectable. Lifecycle races are not. |
| 14 | Tests that re-implement production logic or drive a test-only seam instead of the real path | ~8 (arch 5, ganda 3) | arch 303: tool-list tests compare against a copy of the selection algorithm. ganda 416: tests copy the commit logic inline. ganda 486: only a `CapturingWalk` stub runs, never `HumanMergeWalkHost`. arch 393: tests lock a test-only `StopMessage()`. | none | **no**, apart from "a production member referenced only from tests". |
| 15 | Reflection into non-public members, or by member-name string, where a public API exists or trimming breaks it | 6 (arch 3, nuru 1, taratibu 2) | arch 393 `aspire-app-host/program.cs:765`: reads the internal `ContainerAppDomain` getter by reflection when public `GetOutput` exists. arch 379-004: a test calls the private `OnSearchChanged` by reflection. nuru 556: `UnsafeAccessor` writes the private `Activity._spanId`. taratibu 057: reads `Task<T>.Result` by name, which trimming removes. | partly: IL2075 and friends, only where `IsAotCompatible` is set | **yes**: `BindingFlags.NonPublic`, `[UnsafeAccessor]`, and `GetProperty` or `GetMethod` with a string literal. |
| 16 | Fail-open defaults: a zero enum value that means success, or an empty default that reads as "ok" | 4 (nuru 2, taratibu 2) | taratibu 058: `NodeContractVerdict.Pass = 0`, so `default` reads as Pass. nuru 470-007: `Versions` defaults to `[]`, so `{}` means "never published". nuru 470-complete: `[]` is returned for every non-success HTTP status. | none | **partly**: a zero enum member named Pass, Success, Ok or Allowed. |
| 17 | A stub that reports success around a TODO | 3 (arch 210-post-migration, taratibu 013, jaribu 036) | All three are the same `verify-samples` scaffold: it prints "Samples verified successfully!" around a bare TODO and exits 0. The arch finding also has stale TODOs that name a person. | none | **partly**: a TODO comment in a method whose only other statement is a success message. Low value, because this is one scaffold copied three times. |

**Watch list (2 hits, one repo, or too narrow for its own rule):** incremental-generator models
that hold a `Location`, `ISymbol` or `Compilation` (state 071, 097, 104). Generator emitters that
build string literals without `SymbolDisplay.FormatLiteral` (nuru 470-complete, 508-001). git
`--porcelain` or `--name-only` output parsed without `-z` (ganda 416, 359). `InternalsVisibleTo`
grants that drift from the runfile list (ganda 436, 447, 467, 552-003); that is a build check,
not Roslyn. `default!` or `null!` sentinels in generated code (nuru 478, 508-001, 508-013).

### What this means for an anti-slop rule pack

The largest families, rows 1, 6, 11 and 14 (stale comments, copy instead of reuse, silent
generator skips, tests that re-implement logic), **cannot** be caught by a Roslyn rule, so
review stays the main defense against them. The rule candidates cover rows 2–5, 7–10, 12,
13 and 15. Each candidate is a narrow syntactic slice of its family, not the whole family. The
strongest signal is that agents **silence** the build's existing rules (row 7, and the discard
statements and empty catches in rows 2 and 4). Closing those escape hatches is the best value
per rule.

### Proposed follow-on implement tasks (not filed; await Steven)

Each proposal is one rule in one repo. Ranked by hits, how reliably the rule detects the
pattern, and how few false positives it would raise.

1. **Suppressions must carry a real reason** (row 7, 16 tasks across 4 repos): one TWA rule in
   `timewarp-architecture-convention-analyzers`. It flags `[SuppressMessage]` or
   `[UnconditionalSuppressMessage]` whose `Justification` is empty or `"<Pending>"`, and a
   `#pragma warning disable` that has no trailing reason comment or no matching `restore` in
   the same file. `NoWarn` in props is out of scope for Roslyn; a separate build check could
   cover it.
2. **No warning-silencing discards** (row 2, 5 tasks across 2 repos): one TWA rule. It flags an
   expression statement `_ = <parameter | local | exception | member read>;` that has no side
   effect. Decide alongside it whether to turn IDE0060 back on here, since task 172 set it to
   silent.
3. **No empty or discard-only catch** (row 4, about 6 concrete tasks): one TWA rule. It flags a
   catch block with no statements, or one whose only statement is a discard. It still fires
   when CA1031 is suppressed.
4. **Enable Roslynator RCS0056 line length** (row 9, about 12 tasks): `.editorconfig` only, with
   no new analyzer. The package is already referenced. It catches packed-row cap gaming and
   comment-width nits.
5. **No reflection into non-public members** (row 15, 6 tasks across 3 repos): one TWA rule. It
   flags `BindingFlags.NonPublic`, `[UnsafeAccessor]`, and `GetProperty`, `GetMethod` or
   `GetField` with a string literal, outside an opt-out attribute that requires a reason.
6. **String literal duplicates an accessible const** (row 5, about 4 concrete tasks in a family
   of about 24): one TWA rule, limited to const strings of at least 4 characters in the same
   compilation. False positives are the main risk, so ship it as Info first.
7. **Jaribu tests must be able to fail** (row 3, about 8 concrete tasks): the rule belongs in
   **timewarp-jaribu**, not here. It flags a test method with no Shouldly or Jaribu assertion,
   an assertion on a literal constant, a bare `return` before the first assertion, or
   `Should.Throw<Exception>` / `ThrowAsync<Exception>`.
8. **Test projects ban environment-mutating APIs** (row 12, about 11 tasks): `BannedSymbols` for
   test projects (`Directory.SetCurrentDirectory`, `Environment.SetEnvironmentVariable`,
   `Path.GetTempFileName`, `Environment.Exit`). This is config, not a new analyzer; amuru has
   the most hits.
9. **No `CancellationToken.None` when a token is in scope** (row 13, 2 concrete tasks): one TWA
   rule. It is low on hits, but it is an exact shape.
10. **No control flow on `Exception.Message`** (row 10, 3 tasks, all ganda): one rule in
    timewarp-ganda, or a TWA rule if a second repo shows a hit.

### How to validate

Smoke:

```bash
W=~/worktrees/github.com/TimeWarpEngineering
grep -n 'ShouldBeTrue' $W/timewarp-taratibu/master/kanban/done/013-complete-detailed-code-review-of-timewarptaratibu/review/round-1/merged.md
grep -rn 'Pending' $W/timewarp-architecture/master/kanban/*/210-post-migration*/review/round-*/merged.md | head -3
grep -rln '_ = minimumIgnored' $W/timewarp-ganda/master/kanban/*/552-015-*/review
git -C $W/timewarp-architecture/master log --since=2026-09-01 --name-only --format= -- kanban | grep -cE 'review/.*merged\.md'
git diff --stat origin/master...HEAD -- source tests
```

Expect:

- The first grep prints line 98, `Scaffold_Should_Pass` asserts `true.ShouldBeTrue()` (row 3).
- The second prints the `Justification = "<Pending>"` finding on `global-suppressions.cs`
  (row 7).
- The third lists at least one review file (row 2).
- The `git log` count is in the hundreds (992 on 2026-11-06), which shows the window holds a
  real corpus.
- The final `diff --stat` prints nothing: this task changed no product `source/` or `tests/`.
