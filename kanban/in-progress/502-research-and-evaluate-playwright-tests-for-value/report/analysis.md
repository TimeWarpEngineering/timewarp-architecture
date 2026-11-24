# Analysis: is the Playwright suite worth its cost, and where should it run?

## Executive Summary

Keep the suite and keep it off pull requests. Option 2, nightly only, is already live:
since task 501 the nightly `template-smoke --matrix --app-tests` runs the suite in all five
web-on variants. On 2026-11-23 each variant passed 38/38 in about 590s. In the 130
`ci (playwright)` pull-request runs from 2026-10-29 to 2026-11-24, no browser test failed.
The two red runs were compile errors that `ci (integration)` and template-smoke also
reported. Every real product bug the suite found was in a feature written in the same pull
request as its test.

The gap is at authoring time. Under the walk's gate rules, an implementer who adds or edits
a Playwright class records "not a standalone runfile, CI runs it". Since task 501, no
pull-request job runs it. Follow-up task 523 lets `dev test <file>` run the classes in a
Playwright file, so the walk's per-file gate runs them again.

Do not retire the suite. Do not re-enable a path-gated, sharded pull-request leg now. It
would run on about 80% of product pull requests, and it needs browsers baked into the wslc
runner image.

## Scope

- The suite: `tests/container-apps/web/web-spa-playwright-tests/`. That is 23 browser test
  classes plus one non-browser guard (`screenshot-path-guard-tests.cs`), 40 files, and about
  7.7k lines.
- Where it runs: `.github/workflows/workflow.yml`, `tools/dev-cli/services/test-groups.cs`,
  and `tools/dev-cli/services/template-smoke/template-smoke-test-runs.cs`
  (`AssertGeneratedAppTestSuitesAsync`).
- Overlap: `tests/container-apps/web/web-spa-integration-tests/`,
  `tests/container-apps/web/web-server-integration-tests/`,
  `tests/container-apps/aspire/aspire-tests/`, and co-located runfile tests under
  `source/container-apps/web/`.
- History: every commit that touches the suite, and every kanban task (all columns) that
  mentions Playwright.
- Out of scope: product code edits.

## Methodology

- Read each test class's Purpose and Design regions and test methods, and searched the
  non-browser suites for tests of the same feature.
- Pulled the `ci (playwright)` jobs from the 200 most recent `workflow.yml` runs (created
  2026-10-29 to 2026-11-24) with `gh api .../actions/runs/<id>/jobs`. That gave 130 jobs
  (70 `pull_request`, 60 `push`). For each job: conclusion and duration
  (`completed_at - started_at`). Read the failed logs with `gh run view --log-failed`.
- Read the nightly scheduled run of 2026-11-23 (run 45246814905) for per-variant suite
  timings.
- `git log -- tests/container-apps/web/web-spa-playwright-tests/` (113 commits) and
  `grep -ril playwright kanban/`. Each item was sorted as a real catch, a flake or timing
  fix, a forced update, or a feature test.
- Measured which merged pull requests touch web UI paths: first-parent merges on
  `origin/master` from 2026-10-29 to 2026-11-24, excluding kanban-only merges.
- Runtime baselines from task 500 (`kanban/archived/500-…/report/analysis.md`) and task 514
  (`kanban/done/514-…/report/analysis.md`).

## Findings

### F1. Inventory and overlap

All classes boot an in-process Web.Server through `HostGraphFactory.CreateWebAsync`
(`tests/common/timewarp-testing/host-graph-factory.cs`) on fixed ports, one host at a time.
Each test gets its own host, except the passkey class, which boots one host per class. One
headless Chromium is shared per session (`playwright-browser-session.cs`, task 369). No class
needs Docker, Postgres, or an xAI key, and none is skipped.

| Class | Tests | Proves | Why a browser | Overlap with non-browser tests |
|---|---|---|---|---|
| AgentLinksOnboarding_Given_Wasm | 1 | account id shown, openssl prompt copied | clipboard | partial: web-integ `agent-link-onboarding-prompt-tests.cs` |
| AskFocus_Given_Wasm | 1 | composer focus on open and send; focus returns on close | real focus | none |
| AskMarkdown_Given_Wasm | 1 | safe Markdown; in-app link navigates without a reload | no-reload routing | partial: web-integ `safe-markdown-render-tests.cs` |
| AskPanelDesign_Given_Wasm | 1 | empty-state layout at 1280 and 800 wide | geometry | partial: `ask-starter-prompts-tests.cs` |
| AskSignInRenameNavigate_Given_Wasm | 2 | rename relabels every entry point; `site_map` navigation | passkey sign-in, full stack | partial: `assistant-identity-tests.cs`, `navigation-destinations-tests.cs` |
| AskSignInReprobe_Given_Wasm | 2 | a 401 is not shown as "not configured"; re-probe on sign-in | in-SPA sign-in sequence | partial: `agent-ask-readiness-tests.cs` |
| AskSignInSearchFocus_Given_Wasm | 1 | closing Ask from the palette restores search focus | focus | none |
| AskSurface_Given_Wasm | 1 | Ctrl-K, header actions, edit mode, @ picker, send-button geometry | keyboard, clipboard, layout | partial to high on state: `ask-send-button-tests.cs`, `ask-edit-mode-pill-tests.cs` |
| AskVoiceInput_Given_Wasm | 2 | mic denied, live preview, final text, late transcript ignored | `getUserMedia`, WebSocket | partial: `transcribe-agent-audio-tests.cs` (server side) |
| Breadcrumb_Given_Wasm | 1 | same trail on every route; trail CSS applies | routing, computed style | partial: `page-breadcrumb-trail-tests.cs` |
| ClearXaiApiKey_Given_Wasm | 2 | confirm dialog; Cancel and Escape keep the key; Enter does not confirm | real FluentUI dialog keys | partial: `clear-xai-api-key-confirmation-tests.cs` |
| DarkTheme_Given_Shell | 4 | every surface dark; toggle remembered; dark OS paints dark first | `prefers-color-scheme`, localStorage, computed style | partial: `brand-role-token-tests.cs` (static CSS) |
| DesignSystemCatalog_Given_Wasm | 1 | deep links, filter, unknown slug | routing, DOM | partial: `design-system-catalog-coverage-tests.cs` |
| DesignSystemDrawer_Given_Wasm | 1 | focus trap, Escape, `:modal`, z-order, RTL | focus, `:modal`, paint order | partial: `drawer-render-tests.cs` (markup only) |
| FeedbackAttachmentAdmin_Given_Wasm | 4 | admin grid sort, filters, owner link, narrow width | click, layout | partial: `feedback-admin-state-tests.cs` |
| FeedbackAttachmentPicker_Given_Wasm | 1 | file chooser by click, Enter, Space; spacing tokens | file chooser | none |
| FeedbackAttachment_Given_Wasm | 3 | picked and pasted files attach; text areas size | paste event, file input | partial: upload endpoint tests (server) |
| FirstPaint_Given_PrerenderedShell | 2 + 1 non-browser | prerendered shell styled before WASM; no `<style>` elements | first paint | partial |
| PasskeySession_Given_InteractiveServer | 2 | passkey sign-in leaves the session cookie; `[Authorize]` pages load | virtual WebAuthn | partial: server-integ passkey tests (software authenticator) |
| ProfileEdit_Given_Wasm | 1 | load, cancel, save | WASM state round trip | high-partial: `profile-page-prefill-tests.cs`, `profile-state-tests.cs` |
| RenderModeFooter_Given_Shell | 2 | render-mode icon in Static, Server, and WebAssembly | runtime `RendererInfo` | partial: `render-mode-badge-tests.cs` |
| RolePages_Given_Wasm | 2 | create-only New Role; detail edits; dense trees render | routing, layout | partial to high: `role-form-prefill-tests.cs`, `permission-tree-render-tests.cs` |
| Screenshot_Given_Wasm | 1 | page drawn to PNG; masking; attach as thumbnail | canvas capture | partial: `catalog-agent-screenshot-tests.cs` |
| NavWidth_Given_Shell | 2 | nav collapse and expand remembered; icon full size | layout, localStorage | partial: `shell-breakpoint-tests.cs` |
| StateDevToolsWasm_Given_InspectorEndpoint | 2 | WASM store actions export as OTel spans in Debug only | real WASM export | partial: aspire-tests `state-dev-tools-model-tests.cs` |
| PageLoad_Given_ModelContext | 1 | `navigate` registers during HTML parse | timing before Blazor starts | partial: `catalog-agent-webmcp-tests.cs` |
| TestSources_Should_ (guard) | 1 | no test source writes into `kanban/` | none (file scan) | not a browser test |

"web-integ" is `tests/container-apps/web/web-spa-integration-tests/`. Tests per class are
counted from the source. The nightly test-platform summary reports 38 tests for the project.

No class repeats a non-browser assertion exactly. ProfileEdit and RolePages come closest.
What only a browser can check: focus, geometry, first paint, `prefers-color-scheme`,
clipboard, the file chooser, the microphone, popover light-dismiss, and WASM OTel export.
The project follows the repo's "don't mock your friends" direction: a real host and a real
browser.

### F2. Runtime and CI cost

| Measure | Value | Source |
|---|---|---|
| `ci (playwright)` jobs, 2026-10-29 to 2026-11-24 | 130 (128 success, 2 failure) | gh jobs API |
| Duration of successful jobs | min 397s, median 640s, p90 810s, max 886s | gh jobs API |
| Total runner time | 1,367 minutes over 130 jobs | gh jobs API |
| `ci (fast)` for comparison | 3m 32s to 4m 38s | task 500 report |
| Per-class baseline | `DarkThemePlaywright_` 76.9s, `FeedbackAttachmentPlaywright_` 69.5s and 35.6s | task 500 report, line 121 |
| Pull-request merge wait after task 501 | 11–16m before, 8.4–9.2m after | task 514 report, line 7 |
| Suite when the legs were split (task 366) | about 4.5 min | `kanban/done/366-…/task.md` |
| Nightly suite run per variant, 2026-11-23 | Default 600s, NoPostgres 593s, NoApi 585s, NoGrpc 580s, NoYarp 591s, all 38/38 | run 45246814905 |
| Nightly template-smoke job, 2026-11-23 | 80 min, of which the suite is about 49 min | run 45246814905 |

The suite runs one class after another because every class boots a host on the same fixed
ports. Between task 366 and task 500 its runtime roughly doubled as classes were added. On
pull requests it was the longest job: the one every walk waited on (task 500, Item 2).

### F3. What the suite caught

Real product bugs, counted strictly (the browser test exposed a bug a person could hit):

1. **Task 351** (`80a00365f`, pull request #487). Two product races in Ask Tag pick mode:
   the pick showed before the click listener was armed, and an out-of-order `CancelPick`
   left a pick orphaned. The task says "A fast person could hit this too."
   Browser-only. The code was from task 346, new at the time.
2. **Task 371** (`tooltip-hint.ts`). FluentUI's tooltip calls `showPopover` on a timer and
   light-dismisses the open edit-mode or settings menu. "A person who copies an answer and
   then opens the menu can see the menu shut itself." Browser-only. It was found through a
   test flake in task 370.
3. **Task 295** (pull request #458), two bugs in the feature that pull request added. The
   attachment endpoint returned 415 for `text/plain`, which a server test could have caught.
   The paste module looked up `fluent-text-area` instead of `fluent-textarea`, which only a
   browser shows.

Reproduced but not caught: task 293. A user reported the 401 shown as "not configured"
first. Playwright reproduced it and now guards against it.

Bugs the suite missed: task 296 ("CI missed it": the panel width assertion passes with a
0px page), task 433 (a nav icon too small to see passed), and task 465 (the dark theme bug
was reported by a user; the test was added after the fix).

No `ci (playwright)` failure on record exposed a regression in older code that the pull
request did not touch. In the 130-job window, neither failure came from a browser test. Both
were `SYSLIB1031` compile errors on the task 489-003 branch (runs 45216601900 and
45217698747), and `ci (integration)` and template-smoke reported the same error.

### F4. Upkeep and flake cost

Of the 104 non-merge commits that touch the suite, roughly:

| Category | Count |
|---|---|
| New or extended test shipped with a feature | about 70 |
| Test-side flake or timing fix (`fd411af74`, `b8186f383`, `64abc83e5`, `20ffb440d`, `f66b663b8`, `5052084ad`, `6c37a16d6`, `f26bbc363`, `ef27f338f`) | about 9 |
| Test updated because the product changed on purpose (renames, line-cap splits, package bumps) | about 14 |
| Product fix prompted by a failing Playwright test | about 4 |
| Other (performance, ordering, review rounds) | about 7 |

Recorded flake and cost items:

- Task 434: the drawer test failed on unrelated pull requests #591 and #593 (2026-10-28).
- Tasks 448 and 429: assertions ran while the page still said "Authorizing...".
- Task 359: editing a pull request description re-ran CI and hit the Tag flake.
- Task 372: the stale WASM asset hang.
- Tasks 456 and 498: the Chromium install has to match each `Microsoft.Playwright` bump.
- Task 495: more than 5 minutes of rework on throwaway screenshot tests.
- Task 415: the wslc runner image has no browsers or Node, and runs as non-root with no
  sudo, so `playwright install --with-deps` fails there.

### F5. Where the suite runs today

- **Pull requests:** not at all. Task 501 removed the matrix leg (`51dbd865d`).
- **Nightly:** `template-smoke --matrix --app-tests` (workflow.yml `template-smoke`, schedule
  `17 7 * * *`, hosted `ubuntu-latest`) runs every generated test project, this suite
  included, in each web-on variant. Task 375 set this up. A failed scheduled run emails the
  user who last changed the workflow, and a human files the task (task 375 Notes). The
  nightly has already caught suite-relevant variant breakage (task 376: SmokeNoApi
  `CtrlK_Should_ShowAsk_And_TheKeyState`).
- **Locally:** `dev test --group playwright`, `dev test --project web-spa-playwright-tests
  [--filter-class …]`, and plain `dev test`.
- **Walk gates:** no file in the suite has the `#if !JARIBU_MULTI` runfile guard, so
  `dotnet run --file` cannot run one alone. `dev test` takes no file arguments. Under the
  walk's gate rules, an implementer who adds or edits a Playwright class records "not a
  standalone runfile; CI runs it", and no pull-request job runs it. Every catch in F3 came
  from running the new class on its own pull request. That path is now closed until the
  nightly after merge.
- The `test-groups.cs` Design comment says the group is "local only, CI skips it since task
  501". It does not mention the nightly run.

### F6. How much a path gate would save

There were 40 first-parent merges to master between 2026-10-29 and 2026-11-24 that changed
more than kanban files:

| Path filter | Merges matched |
|---|---|
| `source/container-apps/web/projects/web-spa/` | 11 / 40 |
| `tests/container-apps/web/web-spa-playwright-tests/` | 7 / 40 |
| web-spa, web-server, web-contracts, the suite, `source/libraries/`, `Directory.Packages.props` | 32 / 40 |

A narrow filter (web-spa plus the suite) would miss the changes behind the catches that came
from outside web-spa: FluentUI behavior from a package bump (task 371) and server endpoints
(task 295's 415). A filter wide enough to cover those matches 80% of product pull requests,
so it saves little.

## Options compared

| | 1. Retire and delete | 2. Nightly only (current) | 3. Path-gated and sharded on pull requests |
|---|---|---|---|
| Pull-request wait | none | none | 10–15 min serial on about 80% of product pull requests, or less if sharded |
| Browser-only coverage (focus, paint, popovers, clipboard, file chooser, mic) | lost; nothing else covers it | kept, reported the next morning | kept, reported on the pull request |
| Catches like 351, 371, 295 (new feature, same pull request) | lost | found the next morning, after merge, unless the author runs the class (follow-up 523) | found on the pull request |
| Regressions in older code | none recorded either way | covered by the nightly | covered on the pull request |
| Upkeep (Chromium pins, flake fixes) | removed | stays, off the critical path | stays, and flakes block merges again (434, 359) |
| Infrastructure | none | none new; nightly already runs it 5 times | browsers baked into the wslc image (task 415, timewarp-infrastructure 004) or a hosted leg; port-per-shard split; classify outputs |
| Change size | delete about 7.7k lines and the group | none | workflow, classify, test-groups, image |

## Recommendations

1. **Adopt option 2. It is already in place, so CI needs no change.** Keep the suite.
   Pull requests do not run it. The nightly `template-smoke --matrix --app-tests` runs it in
   every web-on variant.
2. **Close the authoring-time gap (follow-up task 523, priority high).**
   - `dev test` accepts test file paths. A file inside a csproj test project runs that
     project, filtered to the classes declared in the file.
   - With that, the walk's gate ("`./bin/dev test <file…>` is the same run when this
     repository's dev CLI accepts file arguments") runs a Playwright class the change adds or
     edits on the implementer's machine, before the pull request.
   - The same task corrects the `test-groups.cs` and `workflow-command.cs` comments to name
     the nightly run, and adds a line on how to run a Playwright file to the AGENTS.md Tests
     row.
3. **Do not retire (option 1).** The focus, first-paint, popover, clipboard, file-chooser,
   and microphone checks have no non-browser equivalent. The suite found three bugs that no other layer would have seen (351, 371, and
   the `fluent-textarea` tag in 295).
4. **Do not build option 3 now.** Revisit it only if a nightly Playwright failure turns out
   to be a regression that a pull-request gate would have stopped from merging. By then the
   wslc image would need browsers (timewarp-infrastructure 004). No task is filed: there is
   no evidence for it yet.
5. **Not recommended: trimming the nightly to fewer variants.** The suite takes about 49 of
   the nightly's 80 minutes, but the nightly is off the walk's critical path. Task 376 shows
   variant-specific failures do happen.

## References

- `kanban/archived/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md` (Item 2)
- `kanban/done/514-walk-performance-review-for-tasks-489-001-005-495-491-490-453-501-and-503/report/analysis.md`
- `kanban/done/501-disable-ci-playwright-in-workflow-yml/task.md`, commit `51dbd865d`
- `kanban/done/375-run-only-the-default-template-smoke-variant-per-pr-and-the-flag-off-variants-nightly/task.md`
- `kanban/done/376-fix-api-off-and-yarp-off-generated-web-spa-test-failures-found-by-the-nightly-app-tests/task.md`
- `kanban/done/351-…`, `kanban/done/371-…`, `kanban/done/295-…`, `kanban/done/434-…`, `kanban/done/296-…`, `kanban/done/433-…`
- `kanban/archived/415-run-ci-on-the-local-wslc-runner-for-same-repo-push-and-pr-events-forks-stay-on-github-hosted/task.md`
- `tools/dev-cli/services/test-groups.cs`, `tools/dev-cli/endpoints/test-command.cs`, `tools/dev-cli/services/template-smoke/template-smoke-test-runs.cs`
- Nightly run: https://github.com/TimeWarpEngineering/timewarp-architecture/actions/runs/45246814905
