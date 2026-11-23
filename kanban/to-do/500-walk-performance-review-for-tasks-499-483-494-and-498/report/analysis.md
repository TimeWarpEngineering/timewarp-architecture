# Walk Performance Analysis: `timewarp-architecture` Tasks 499, 483, 494, and 498

## 1. Executive Summary

Across the last four finished `ganda task work` walks in `timewarp-architecture` (**499**, **483**, **494**, and **498**), total active execution time was **5h 50m 49s** (`350.9m`, plus `3h 03m 58s` of blocked/unblock pauses across an `8h 54m 47s` wall span). SigNoz OTEL spans, walk logs, and agent transcripts point to two large systemic taxes paid by **every** walk — **10–15 minutes per PR** waiting on `ci (playwright)` and **~4–5 minutes per walk** in redundant `ganda repo attest`/`audit` runs triggered by `.githooks/` and post-review re-audits — alongside a silent **~34-minute** `roslynk` background `dotnet restore` storm on every Grok launch (already mitigated by uninstalling `roslynk`) and several walk-specific rework loops (Task 494 master-red re-run, Task 498 Aspire 17 `IngressSmoke` timeout loop and OpenCode 429 quota crash).

Task **499** ran **after** today's `ganda` update (`1.0.0-beta.38+c8f6a9ca`, including Task 707's recorded-pass audit reuse, installed `2026-11-23T06:41:34Z`), confirming which `ganda` bottlenecks remain live today.

---

## 2. Scope

- **Analyzed walks (finished & merged):**
  - **Task 499** — PR [#656](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/656): line-cap baseline + restore `source-file-line-cap` to error
  - **Task 483** — PR [#655](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/655): `[ContractState]` generated contract-bound action sets
  - **Task 494** — PR [#654](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/654): EF Core & Npgsql `11.0.0-rc.3` → `11.0.0` GA
  - **Task 498** — PR [#653](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/653): NuGet pin currency + Aspire 17 migration
- **Excluded (live at analysis time):** Task **489-001** (`trace 4e56755d912282270f3a9f4f5555e4af`, started `2026-11-23T10:25:09Z`, still `alive` per `ganda kanban who`).
- **`ganda` version boundary:**
  - Installed binary: `1.0.0-beta.38+c8f6a9ca16de7fb7f6fd9c59a1ff75ada1472b1b` (`land kanban 707`), installed `2026-11-23T06:41:34Z` (`13:41:34 +0700`).
  - Tasks **498**, **494**, and **483** ran on `2026-11-22` (pre-707).
  - Task **499** ran on `2026-11-23T08:31:55Z` (`15:31:55 +0700`) — **post-707**. Items observed on Task 499 are marked **Still live on beta.38**.
- **Already mitigated during review (2026-11-23):**
  - `roslynk` (`3.0.0`) uninstalled globally (`dotnet tool uninstall --global Roslynk`).
  - Unauthenticated Cloudflare MCP servers disabled for Grok.

---

## 3. Methodology

- **SigNoz OTEL MCP:** Queried `ganda`, `claude-code`, `dotnet-cli`, and `TimeWarp.Jaribu` spans across all 7 trace IDs (`139,934` spans scanned), plus 7-day org-wide `taratibu.node*` p50s and `ganda.audit.check` totals (`1.79M` spans scanned).
- **Walk evidence folders:** Read `walk.console.log`, `task-work.progress.log`, `signals.journal.json`, `oracle-*.log`, and `*.transcript.txt` under `~/.timewarp/ganda/walks/TimeWarpEngineering/timewarp-architecture/{499,483,494,498}/`.
- **Agent transcripts:** Parsed Claude JSONL transcripts, Grok `events.jsonl` / `chat_history.jsonl`, and OpenCode SQLite DB (`~/.local/share/opencode/opencode.db`) programmatically with `python3` (all timestamps converted to UTC `Z`).
- **PR & CLI telemetry:** Inspected `gh pr view` check timings/files, `ganda usage stats --since 2026-11-16`, `.githooks/*.cs`, `tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`, `.github/workflows/workflow.yml`, and `timewarp-ganda` source (`kanban-land-service.cs`, `audit-kanban-dependence.cs`, commit `3d8e8aa9e`).

---

## 4. Measured Walk & OTEL Baseline

### 4.1 Walk Summary (UTC, Measured)

| Task | PR | UTC Window | Wall Span | Active Time | Resumes / Retries | SigNoz Trace ID(s) |
|---|---|---|---|---|---|---|
| **499** | [#656](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/656) | `2026-11-23T08:31:55Z` → `09:10:01Z` | `38m 06s` | `38m 06s` | 0 resumes; 1 clean review | `0cbe6c95cebb8884146a8c9dafb9246b` |
| **483** | [#655](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/655) | `2026-11-22T10:39:30Z` → `11:58:22Z` | `1h 18m 52s` | `1h 18m 52s` | Resumed from Nov 7 block; 2 review rounds (M1, M2 fixed) | `c1e0682d592c5553685cabb93b5d8944` |
| **494** | [#654](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/654) | `2026-11-22T06:02:08Z` → `10:09:35Z` | `4h 07m 27s` | `1h 10m 40s` | Blocked `2h 56m 47s` waiting for 498; full `implement` re-run; 1 clean review | `7c7c708549d50aa42afb243a03677b7d`, `920b7dfb38a9082c37c7ca50de5e0a60` |
| **498** | [#653](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/653) | `2026-11-22T06:36:39Z` → `09:27:01Z` | `2h 50m 22s` | `2h 43m 11s` | Run 1 429 (`9m 21s`), Run 2 Aspire 17 block (`12m 18s` + `7m 11s` pause), Run 3 (`101m 47s`), review-1a refused (`15s`), 2 review rounds (M1, M2 fixed) | `e61297108ce255d0f17b968d7c03836a`, `420632c2dbe2b1bf53667cd96753f83d`, `61bbdb77ae97f45b1b16dd028a4aa131` |
| **Total** | **4 PRs** | — | **8h 54m 47s** | **5h 50m 49s** | **4/4 merged** | 7 traces |

### 4.2 Per-Step Breakdown (Measured)

| Stage / Step | 499 | 483 | 494 (run 1 + run 2) | 498 (run 1–3) | 4-Walk Total | Org 7d SigNoz p50 |
|---|---:|---:|---:|---:|---:|---:|
| Pre-walk `task work` startup | `25.5s` | `25.0s` | `36.4s` (run 2) | `35.9s + 30.2s + 29.4s` | `3m 02s` | — |
| `claim` + `land-claim` | `23.3s + 48.1s` | `9.6s + 0.2s` | `26.1s + 47.5s` | `27.6s + 50.5s` | `3m 53s` | `13.1s + 11.9s` |
| `implement` (initial runs) | `11m 21s` | `38m 56s` | `27m 07s + 23m 37s` | `9m 21s + 12m 22s + 101m 47s` | `3h 44m 31s` | `4m 19s` |
| `review` loop (incl. fix passes) | `6m 05s` | `21m 32s` | `54.2s` | `22m 15s` | `50m 46s` | `9m 03s` |
| `audit` (host compute) | `1m 34s` | `1m 34s` | `1m 51s` | `1m 37s` | `6m 36s` | `37.3s` |
| `open-pr` + `land-pr` + `human-merge` | `19.6s` | `20.4s` | `18.8s` | `19.1s` | `1m 18s` | `18.5s` |
| `merge` (CI wait + pre-merge audit) | `16m 26s` | `14m 41s` | `12m 44s` | `11m 22s` | `55m 13s` | *(no span)* |
| `kanban-done` + `land-done` + `gc` | `53.0s` | `1m 31s` | `1m 35s` | `1m 29s` | `5m 28s` | *(no span)* |

### 4.3 `ganda usage stats --since 2026-11-16` (Org-Wide Frequency)

| Command | Invocations | Errors | Error Rate | p50 |
|---|---:|---:|---:|---:|
| `repo attest` | `3,071` | `500` | `16.3%` | `1.2s` |
| `repo audit` | `1,071` | `98` | `9.2%` | `33.7s` |
| `kanban claim` | `263` | `14` | `5.3%` | `48.4s` |
| `kanban publish` | `258` | `11` | `4.3%` | `46.1s` |
| `task work` | `194` | `51` | `26.3%` | `25m 49s` |

---

## 5. Findings (Walk-Through Items — One per Section)

Each item below is standalone so we can decide on them one at a time.

---

### Item 1A — `home fast-forward` fires `.githooks/post-merge.cs` (`39–52s` every time)
- **Repo:** `timewarp-ganda`
- **Status:** Open — **still live on `ganda 1.0.0-beta.38` (Task 499 today)**
- **Time lost:** `6m 52s` (`412s`) across 9 fast-forwards in 4 walks — **`~1.5–2m` paid by every walk** (measured).
  - `land-claim`: `39s` (499), `38s` (494), `41s` (498)
  - `audit`: `48s` (499), `41s` (483), `42s` (494), `52s` (498)
  - `land-done`: `50s` (483), `52s` (494), `45s` (498)
  - Also observed live while creating this task (`ganda kanban claim 500` took `80s`).
- **Why it happens:** Landing kitchen updates onto `master` runs `git merge --ff-only` in origin-home (`timewarp-ganda/source/timewarp-ganda/services/kanban-land-service.cs:381`). That triggers `.githooks/post-merge.cs`, which boots a `dotnet` runfile and runs a full `ganda repo attest` on `master`. `.githooks/post-merge.cs:25` already checks `GANDA_ATTEST_HOOK=0`, but `ganda` never sets it during `home fast-forward`.
- **Proposed fix:** Pass `GANDA_ATTEST_HOOK=0` inside `ganda`'s `TryFastForwardHomeAsync` (and board-only kitchen lands) so fast-forwarding `master` doesn't boot `dotnet` + re-audit `master` 2–3 times per walk.

---

### Item 1B — `merge` re-runs `ganda repo audit` / `package-pin-currency` 42s after `audit` just ran it
- **Repo:** `timewarp-ganda`
- **Status:** Open — **still live on `ganda 1.0.0-beta.38` (Task 499 today despite Task 707)**
- **Time lost:** `~20–30s` per walk (measured). Across 4 walks, `package-pin-currency` alone ran twice per walk for `82.6s` (`8.5–19.0s` per call); org-wide 7-day SigNoz totals for the top 4 checks are `dev-audit` (`6.2h`), `task-live-dead-end` (`5.0h`), `package-pin-currency` (`4.1h`), and `kanban-landed` (`1.1h`).
- **Why it happens:** On Task 499 (`trace 0cbe6c95...`), `taratibu.node audit` ran `ganda repo audit` at `08:52:08Z` (`13s`, `package-pin-currency` `9.35s`), and **42 seconds later** at `08:52:50Z`, `merge` ran `ganda repo audit` again (`13s`, `package-pin-currency` `8.76s`) on the same tree. Task 707's recorded-pass reuse only hits when a signed attestation note exists for that exact tree SHA; `taratibu.node audit` runs `repo audit` (not `repo attest`), so no signed note exists when `review/` commits or `audit --fix` changed the tree.
- **Proposed fix:** Have `taratibu.node audit` attest the passing tree so `merge` gets a Task 707 reuse hit, and add a short-TTL (~10m) cache for `package-pin-currency` NuGet feed queries.

---

### Item 1C — Intermediate WIP `git commit`s inside `implement` fire `.githooks/post-commit.cs` (`45–154s`/commit)
- **Repo:** `timewarp-ganda` / implementer oracle brief
- **Status:** Open
- **Time lost:** `~13m` across 4 walks (`45–154s` per commit; measured), plus **`3m 21s`** of extra rebuild/audit/commit rework on Task 499 (`#26` `125s` failed `post-commit` attest on `TimeWarp.Nuru.DevCli`).
- **Why it happens:** Every `git commit` runs `.githooks/post-commit.cs` → full `ganda repo attest`. Multi-commit implementations (Task 483 made 5 commits; Task 498 made 4) pay the hook cost repeatedly, and intermediate commits with dirty `task.md` (494) or un-fixed baselines (498 `#149`, `118s`) fail attestation (`16.3%` org-wide error rate across `3,071` `repo attest` invocations).
- **Proposed fix:** Allow/instruct implementers to pass `GANDA_ATTEST_HOOK=0` on intermediate WIP commits and attest once on the final tip commit (or let the host `audit` node own the single attestation).

---

### Item 2 — `ci (playwright)` serial bottleneck on every PR (`10–15m` per PR)
- **Repo:** `timewarp-architecture`
- **Status:** Open (Steven leaning toward disabling Playwright in CI + filing a research task to evaluate value)
- **Time lost:** **`~50–55m` across 4 PRs (`10–15m` per PR, measured)** — 100% of walks wait here.
  - PR #656 (Task 499, line-cap baseline JSON + 4 JS region comments): `ci (fast)` `4m 38s`, `ci (playwright)` **`14m 46s`** (`08:54:01Z` → `09:08:47Z`).
  - PR #655 (Task 483): `merge` `14m 41s` (`ci (playwright)` ~14m).
  - PR #654 (Task 494, EF Core + Npgsql pins only): `ci (fast)` `3m 54s`, `ci (playwright)` **`11m 02s`** (`09:56:28Z` → `10:07:30Z`).
  - PR #653 (Task 498): `ci (fast)` `3m 32s`, `ci (playwright)` **`9m 58s`** (`09:15:09Z` → `09:25:07Z`).
  - Also `detect-paths` adds `62–79s` before any matrix job starts.
- **Why it happens:** `.github/workflows/workflow.yml:216-217` runs `ci (playwright)` on every PR regardless of whether UI/web-spa files changed, and Playwright classes (`DarkThemePlaywright_` `76.9s`, `FeedbackAttachmentPlaywright_` `69.5s` + `35.6s`, etc.) execute serially.
- **Proposed tasks (per Steven's note):**
  - **2A:** Disable `ci (playwright)` in `.github/workflows/workflow.yml` (cuts PR merge wait from `11–15m` down to `~5–7m`).
  - **2B:** Research task to evaluate whether the Playwright suite is worth keeping (bugs caught vs unit/integration coverage; retire, nightly-only, or path-gate + shard).

---

### Item 3 — Silent `roslynk` background `dotnet restore` storm + broken Cloudflare MCPs on Grok launches
- **Repo:** `timewarp-flow` / `roslynk`
- **Status:** **Mitigated by Steven on 2026-11-23** (`dotnet tool uninstall --global Roslynk`; Cloudflare MCPs disabled)
- **What SigNoz caught (for the record if `roslynk` is re-added later):**
  - Every Grok launch in a fresh `timewarp-architecture` worktree connected `roslynk`, which immediately ran `app restore` (`455–512s`) + `dotnet restore` (`219–264s`) across ~70 projects in the background — even when Grok never called a single `roslynk` tool (Task 499 Grok `review-1`, `span 07451cb86ef6146c`: `4.45s` total tool calls vs `71+` `app restore` spans totaling `458.2s`!). Total across 4 walks: **`23m 10s` `app restore` + `11m 12s` `dotnet restore`** of background CPU/NuGet lock contention.
  - 7/7 Grok sessions also failed OAuth on 4 Cloudflare HTTP MCP servers (`cloudflare-api`, `cloudflare-observability`, `cloudflare-builds`, `cloudflare-bindings`), adding `1.4–3.0s` startup noise per launch.
- **Note if re-enabling later:** Require lazy solution loading on first `roslynk` tool call (never eager restore on MCP startup), and exclude `roslynk` from headless reviewer configs that don't invoke it.

---

### Item 4A — Master-audit pre-flight before launching `implement`
- **Repo:** `timewarp-ganda`
- **Status:** Open
- **Time lost:** **`28m 07s` active rework + `2h 56m 47s` wall wait** on Task 494 (measured).
- **Why it happens:** Task 494 claimed and launched `implement-1` while `origin/master` was already red on `ganda repo audit` (owned by unmerged sibling Task 498). `implement-1` edited files, built (`35s`), and ran a `12m 08s` `template-smoke --matrix` (unnecessary on a non-`#if` NuGet pin bump) *before* running `ganda repo audit` at `06:21:59Z`, then spent ~6m deliberating before returning `Blocked`. After 498 merged 3 hours later, `implement-2` re-ran everything (`23m 37s`, including a second `12m 29s` `template-smoke --matrix`).
- **Proposed fix:** Run a fast `origin/master` audit pre-flight (or reuse master's attestation status) before launching `implement`; also instruct implementers to check `ganda repo audit` before `template-smoke` and only pass `--matrix` when `#if` flags or template symbols change.

---

### Item 4B — Provider 429 quota cooldown file
- **Repo:** `timewarp-ganda`
- **Status:** Open
- **Time lost:** **`10m 31s`** on Task 498 (`9m 21s` run + `70s` resume gap, measured).
- **Why it happens:** `ganda` dispatched Task 498 `implement-1` to `implementer-opencode` (`grok-4.7`), which worked for `9m 21s` then died with HTTP 429 `GoUsageLimitError` (*"OpenCode Go weekly usage limit reached, resets in 17h 13m"*). Switching to Claude forced re-discovery of `AMURU0001` build failures.
- **Proposed fix:** When any provider exits with a quota/rate-limit error carrying a reset window, write a cooldown timestamp under `~/.timewarp/ganda/` so `ganda usage pick` skips that provider until reset.

---

### Item 5 — `IngressSmoke` `SetupOnce` (`35m 47s` debug timeout loop on Task 498 vs `38.5s` when healthy)
- **Repo:** `timewarp-architecture`
- **Status:** Open — needs Steven's decision (Option A vs Option B)
- **What `IngressSmoke` tests (`tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`):**
  Closed-box end-to-end boot of the full Aspire AppHost graph (`web-server`, `api-server`, YARP `ingress`, ephemeral Postgres, `web-migrations`, `StateDevTools`, zero mocks). Specifically guards regressions unit tests missed:
  1. HTTP traffic actually flowing through YARP to `web-server` (guarded after a 2026-07-22 TLS/502 bug where backend health checks passed but proxied requests failed).
  2. Generated `/api/*` route carve-outs (`WebServerApiRoutePrefixes`: `/api/identity`, `/api/Roles`) reaching `web-server` through YARP.
  3. Trusted `X-Forwarded-Host` / `X-Forwarded-Proto` proxy headers and passkey RP ID selection.
  4. First boot against an empty Postgres DB logging zero `Error` lines while `web-migrations` runs concurrently.
  5. `StateDevTools` starting and serving its UI.
- **Why it burned `35m 47s` on Task 498 (unlike Playwright, NOT slow when healthy):**
  - When passing, `IngressSmoke` takes **`38.5s`** (measured in Task 498 once fixed), and `ci (integration)` finishes in `~5–7m`.
  - On Task 498, Aspire 17 broke `web-server`'s port/environment, and `ingress-smoke-tests.cs:72` hardcodes a **4-minute (`TimeSpan.FromMinutes(4)`)** readiness budget (to cover EF Core 11 `dotnet ef --verbose` ~90s + boot seed up to 180s). Every failed debug attempt sat silently for `4m 28s–4m 46s` before timing out (`11` failing `SetupOnce` spans = `35m 47s` in SigNoz).
  - Additionally, Claude chained `dotnet build ... ; ./aspire-tests` with `;` instead of `&&` on `#101` and `#107`, burning **`9m 16s`** running stale binaries after analyzer build failures (`IDE0008`, `RCS1037`). A stale `~/.aspire/bundle` (DCP `0.23.6`) burned another `5m 04s` before `aspire setup` was run.
- **Decision options:**
  - **Option A (keep enabled, fast-fail):** Keep `IngressSmoke` active (it's ~38s when healthy and guards real wiring), add a task to fail fast in `SetupOnce` when a resource exits/crashes + verify `~/.aspire/bundle` version in `dev build`/`AspireRun`.
  - **Option B (disable + research):** Disable `IngressSmoke` now and file a research task to evaluate its value and investigate why EF migrations / Aspire startup requires a 4-minute timeout.

---

### Item 6A — `25–36s` pre-walk `task work` startup delay & `110` `ganda.claims.fetch` calls
- **Repo:** `timewarp-ganda`
- **Status:** Open — **still live on `ganda 1.0.0-beta.38` (Task 499 today)**
- **Time lost:** **`~5m` across 4 walks** (`3m 02s` pre-walk startup across 6 launches + `1m 56s` across `110` `ganda.claims.fetch` calls, measured in SigNoz).
- **Why it happens:** Every `task work` trace spends `25.0s–36.4s` before `taratibu.node` #1 begins. On Task 499 (`trace 0cbe6c95...`), there is an uninstrumented **`20.0s` gap** (`08:31:30.8Z` → `08:31:50.8Z`), followed by **4 consecutive `ganda.claims.fetch` calls** (`1.2s` each) before node 1, plus 14 more during `audit`, `merge`, and `gc`.
- **Proposed fix:** Instrument the 20s startup gap in `ganda task work` and coalesce/cache `claims.fetch` within a single `ganda` process instead of running `git fetch` 4 times in 4 seconds.

---

### Item 6B — Missing SigNoz spans on the merge tail + broken `timing total` log math
- **Repo:** `timewarp-ganda`
- **Status:** Open — **still live on `ganda 1.0.0-beta.38` (Task 499 today)**
- **Impact:** Observability blind spot of **`60m 49s`** (`12.9–17.3m` per walk) across the 4 walks.
- **Why it happens:**
  1. `ganda.task.walk` and `taratibu.run task-work` end at `human-merge`. Everything after (`merge`, `kanban-done`, `land-done`, `gc`) emits zero `taratibu.node` spans in SigNoz — only flat `ganda.claims.fetch` and `ganda.audit.check` spans directly under `task work`.
  2. `walk.console.log` / `task-work.progress.log` floors durations (`101m 45s` prints as `1h`, hiding 41m; `111s` `audit` prints as `1m` right beside `108s` of named waits) and double-counts review substeps (`review` + `implement-fix-2` + `review-2`, inflating `timing total` by `7m` on 483 and `11m` on 498).
- **Proposed fix:** Emit `taratibu.node` spans for `merge`, `kanban-done`, `land-done`, and `gc`, and fix duration formatting + nested-step summing in the walk timing summary.

---

### Item 7 — Hand Off to `/tw-correct` (Repeated Agent Command Mistake)
- **Repo:** `timewarp-flow` (oracle brief / command guidance)
- **Status:** Hand off to `/tw-correct` (per `tw-walk-retro` §7 — not a proposed task here)
- **Class:** Piping `dev build` or `ganda repo audit` directly into `tail`/`grep` without saving output to a file, truncating diagnostics/exit codes and forcing full re-runs.
- **Evidence (3 occurrences across 2 tasks, `1m 42s` wasted, measured):**
  - Task 499 (`#10` → `#11`): `ganda repo audit 2>&1 | grep -i -A4 'line-cap'` discarded non-line-cap failures, forcing a `36.0s` re-run.
  - Task 483 (`implement-fix-2` `#7` → `#8`): `./bin/dev build 2>&1 | tail -5` cut off `IDE0011`, forcing a `12.0s` re-run.
  - Task 483 (`implement-fix-2` `#11` → `#12`): `ganda repo audit 2>&1 | tail -15` cut off the exit code/summary, forcing a `54.3s` re-run.

---

### Item 8 — Lower-Impact / Review & Hygiene Findings
- **Grok reviewer latency vs Claude (review value check):** Grok caught real defects on 483 (M1 `analyzer-package-split.md` stale ID range; M2 `HasMockFactory` visibility/signature mismatch) and 498 (M1 `release` CI job missing Aspire CLI install; M2 `AspirePublish`/`AspireDeploy` `MinimumCliVersion` left at `13.6`). However, Grok spends **98.7–99.4%** of its wall time in model reasoning (`6m 05s` on tiny config Task 499 vs Claude's `54s` on tiny NuGet Task 494; org 7d SigNoz p50 `review` is `9m 03s` vs `implement` `4m 19s`), and in 498 `review-1a` (`15s`) emitted turn-1 placeholder JSON with 0 tool calls and had to be relaunched.
- **Stale PR titles on resumed/merged branches (`timewarp-ganda`):** `open-pr` titled PR #655 `docs(kanban): record task 483 Mediator blocker...` (a Nov 7 commit) and PR #653 `Merge remote-tracking branch 'origin/master'...`.
- **Undeclared transitive major bumps (`timewarp-ganda`):** Task 498 `implement-2` burned `12m 18s` + `7m 11s` human pause hitting Aspire 17 pulled transitively by `TimeWarp.State.DevTools.Hosting 12.0.0-beta.19`.

---

## 6. Recommended Walk-Through Order

1. **Item 2 (Playwright CI)** — largest guaranteed per-PR savings (`10–15m`/PR).
2. **Item 1A & 1B (`ganda` `home fast-forward` hook + post-audit reuse)** — confirmed still live on today's `beta.38`, hits 100% of walks (`~2m+`/walk).
3. **Item 5 (`IngressSmoke` `SetupOnce`)** — decide Option A (keep + fast-fail) vs Option B (disable + research).
4. **Item 1C (WIP `post-commit` hook)** — removes `45–154s`/commit tax and 16.3% `repo attest` failure rate.
5. **Item 4A & 4B (`ganda` pre-flight master audit + 429 cooldown)** — prevents 10–28m wasted walks.
6. **Item 6A & 6B (`ganda` startup/fetch + merge-tail OTEL spans & log math)** — closes startup tax and 13–17m SigNoz blind spot.
7. **Item 7 (`/tw-correct`)** — run `/tw-correct` on piped `build`/`audit` truncation.
8. **Item 8 (review effort/model routing, PR titles)** — polish.

---

## 7. References

- Walk evidence: `~/.timewarp/ganda/walks/TimeWarpEngineering/timewarp-architecture/{499,483,494,498}/`
- PRs: [#656](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/656) (499), [#655](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/655) (483), [#654](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/654) (494), [#653](https://github.com/TimeWarpEngineering/timewarp-architecture/pull/653) (498)
- Hooks & tests inspected: `.githooks/post-merge.cs`, `.githooks/post-commit.cs`, `.github/workflows/workflow.yml`, `tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`
- `timewarp-ganda` source inspected: `services/kanban-land-service.cs`, `services/audit/audit-kanban-dependence.cs`, commit `3d8e8aa9e` (Task 707)
