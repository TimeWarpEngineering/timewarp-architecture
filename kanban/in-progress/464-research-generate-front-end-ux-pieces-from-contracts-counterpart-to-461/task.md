# Research: generate front-end (UX) pieces from contracts — counterpart to 461

## Description

Research task (proposal only, no implementation). This is the front-end counterpart to task 461
(`kanban/in-progress/461-research-infer-role-aggregate-from-contracts-contract-driven-aggregate-generation`).
Using the Roles slice, decide which SPA pieces can be source-generated from the contracts:
- the typed API surface screens call (action sets over `DefaultApiHandler`)
- the DTOs consumed and the fetchers for server-bound state
- screen states (loading, error, success)
- `[Destructive]` confirm
- mocks
- form bindings and validation
- CRUD scaffolding


Kitchen deliverable: `proposal.md` (current master, options A–D as code). The older draft
`contracts-frontend-example.md` (written against `f003919db`) is superseded; it is Option C.

## Outcome of 461 (read first; Steve 2026-11-05)

461 is resolved. There is **no** entity generator: domain is modeled first by hand, and
`TWA0039–TWA0043` (task 474, `contract-agreement-analyzer.cs`) prove that an aggregate satisfies
its operation group's contracts. Glue generation in the application layer was spiked (476,
`kanban/done/476-*/proposal.md`) and Steve chose "analyzer alone" because only 3 mutation calls
were mechanical and by-name mapping had a real leak case. The ballot record is
`kanban/in-progress/461-*/rfc/rfc.md`.

What that means for this task:

- Do not assume a back-end generator exists to coordinate with. The SPA side stands alone.
- Options must be **code documents** in the kitchen (input the developer writes, what is
  generated, what stays hand-written), not prose. Steve compares code, not descriptions.
- Hooks that must be written are **extended partial methods with an access modifier** (missing
  body is CS8795). `partial void` is silent and only fits optional hooks.
- Consider an **analyzer-first** answer here too: for each candidate piece, say whether a build
  check that proves agreement (contract ↔ action set, DTO, mock, validator) is enough before a
  generator is justified. Count the mechanical pieces in Roles the way 476 did.
- Roles is now a real aggregate (473) and Profile/Roles contracts were renamed to match
  (`DisplayName`, `Permissions`); read current master, not the `f003919db` draft.
- Proposal only. Stop at the options and Steve's question; do not split implementation tasks.

## Goal (implementer restatement)

Write a proposal that Steve can decide from. It covers which SPA pieces for the Roles slice could be
built from the contracts, using the current master code. No product code changes.

Acceptance criteria:

- A kitchen `proposal.md` built on current master, not the `f003919db` draft. It contains:
  - an inventory of the Roles SPA action sets that counts the mechanical pieces the way 476 did
  - each option as a code document: what the developer writes, what is generated, what stays
    hand-written
- Each candidate piece (action sets, fetchers and snapshots, screen states, `[Destructive]`
  confirm, mocks, forms and validation, CRUD scaffolding) has an analyzer-first answer: is a build
  check that proves agreement enough before a generator is justified?
- Every hand-written piece in a generator option has a required hook or a failing check. Required
  hooks are extended partial methods with an access modifier (CS8795). Optional hooks are
  `partial void`.
- Facts that limit the options are stated with paths.
- The proposal ends with Steve's question. No option is picked and no implementation tasks are
  split.

## Boundary

- Visuals and layout stay hand-written (Razor markup, CSS, copy). The generator emits no markup.
- Purely local state and actions that never touch the server (e.g. `SetPermissionSelectedActionSet`
  draft toggles) stay hand-written, and the generator must not know about them.

## Requirement: no implicit hand-written pieces (same rule as 461)

Every hand-written piece gets a generated partial hook or a stub that fails until filled in:
- `ConfirmXxxAsync` for `[Destructive]` operations is a required partial method, so the build fails until it's written.
- Generated page code-behind without `.razor` markup triggers an analyzer error.
- A missing mock factory triggers an analyzer warning.
- Optional `partial void` hooks: `OnBuildingRequest`, `OnSucceeded`, `OnXxxLoaded`.

## Findings so far

- Already generated or derived today:
  - ActionSet dispatch methods (TimeWarp.State)
  - `[StateAccess]` accessors
  - `[Page]` routes and policy
  - `ActionRoster`
  - the mock factory registry
  - the `[ReferenceableResource]` registry
  - TWA0022 and TWA0026 enforce dispatch through actions.
- Hand-written today:
  - every server-bound `XxxActionSet` (GetRequest + HandleSuccess)
  - success flags (`LastCreatedRoleId`, `LastSetRolePermissionsSucceeded`)
  - `ConfirmAsync` overrides
- Gap: the `UpdateRole`, `DeleteRole` and `GetRole` contracts have no SPA action sets or screens.

## Checklist

- [ ] Review `proposal.md` with Steven
- [ ] Decide the opt-in shape (`[ContractState(...)]` on the state vs per-contract attribute)
- [ ] Decide `ContractResult<T>` slots vs keeping hand-written success flags
- [ ] Decide conflict rule with existing hand-written action sets (generate-if-absent vs require removal)
- [ ] Decide refetch/invalidation declaration (default none)
- [ ] Confirm every hand-written piece has a generated hook or failing stub
- [ ] Steve answers the question in section 7 of `proposal.md`

## Session

- Created: 2955984 (2026-11-02)
- 2026-11-05 implementer (ganda task work): read current master Roles SPA, contracts, generators,
  and the 476 proposal. Wrote `proposal.md` with options A–D as code. Marked
  `contracts-frontend-example.md` as superseded. No product code changed.
- `ganda repo audit` exits 0 with two warnings that were already there before this task:
  - `kanban-landed`: 9 live claims on other tasks have no worktree here. I did not run `--fix`,
    because it would act on other tasks' claims, which may be held on another machine.
  - `source-file-line-cap`: 3 EF migration designer and snapshot files, plus one kanban research
    runfile. The check is at warning severity since 469-001, and TWA0038 enforces the cap.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session a6b13f79-d197-4902-975d-32873d675714, max-turns 500, Done — 2026-11-05T15:43:51Z
- Review oracle round 1 (2026-11-05): tw-implementation-review, effort 2, roster general (claude,
  model claude-opus-5-5, headless ganda task work, turn cap 120). The restatement matches the task,
  so no misread was recorded. The falsifiable claims in sections 2–4 were re-run and hold. There are
  4 open findings (2 suggestions, 2 nits), so a fix pass is requested. Disposition is pending.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 4375f6c0-13ea-40f7-bcc1-53e385e3160f, max-turns 120, artifacts general.md, merged.md — 2026-11-05T15:47:14Z
- 2026-11-05 implement-fix-2 (implementer-claude): fixed M1–M4 from `review/round-1/merged.md`.
  Re-ran the greps behind the counts: `action.Command` hits 6 files, 5 of them `DefaultApiHandler`s;
  19 files override `HandleError`; 16 `DefaultApiHandler`s take a `Query`. `HandleError` is abstract
  on `ApiHandler` and called from the private `HandleApiResponseAsync`, which is why D needs the
  `OnProblem` virtual. No product code changed.
- Review oracle round 2 (2026-11-05): tw-implementation-review, effort 2, roster general (claude,
  model claude-opus-5-5, headless ganda task work, turn cap 120). It reviewed the fix delta only.
  M1–M4 are verified fixed. The restatement still matches the task, so there is no misread. One new
  finding is open (M5, a suggestion), so a fix pass is requested. Disposition is pending.
- 2026-11-05 implement-fix-3 (implementer-claude): fixed M5 from `review/round-2/merged.md` in
  Option D of `proposal.md`. Read `api-handler.cs`, `default-api-handler.cs`, and
  `http-api-service.cs`. `HttpClient` exceptions are not caught in `HttpApiService`, so they reach
  the rethrow at `api-handler.cs:87-92`. No product code changed.

## Results

`proposal.md` in this kitchen is the deliverable. Findings:

- **Mechanical pieces in Roles:** 4 server-bound handlers. 2 of them have mechanical request builds
  (Create, Fetch). 4 success writes are mechanical (1 snapshot, 3 flags). 3 required hand-written
  pieces read or seed local drafts. That is about 12 lines of mechanical body. SPA-wide, only 5 of
  41 `DefaultApiHandler`s build the request from the contract Command on the Action.
- **Feasibility limit:** Roslyn generators cannot see each other's output. A generated action set
  gets no dispatch method from TimeWarp.State, no handler registration from TimeWarp.Mediator, and
  no roster entry. A generated `[Page]` partial gets no route. Only bodies inside hand-written
  partial types (Option B) work. The old draft's whole-set generator is Option C.
- **Hazard case:** if draft seeding in FetchRoles were an optional hook and nobody wrote it, Save
  would post an empty permission list and strip every non-Administrator role. No test covers that
  path. Any generator must make that hook required (CS8795).
- **Analyzer-first candidates (Option A):** A1 SPA contract coverage (it flags GetRole, UpdateRole,
  and DeleteRole), A2 `[Destructive]` ⇔ `ConfirmAsync` (0 contracts use `[Destructive]`, 2 handlers
  confirm), A3 `[CatalogAction]` ⇔ contract policy and description (CreateRole's description
  already differs), A4 mock coverage (today a missing factory falls through to the real API with
  only a Debug log).
- **Option D:** `ContractResult<T>` slots replace the 9 hand-written success flags and ids, and the
  sticky `LastCreatedRoleId` comparison in `RoleForm.razor`. The base `ApiHandler` gets two empty
  virtuals. `OnStarted` sets `Running` and runs only after a non-null `GetRequest`. `OnProblem` sets
  `Failed` and runs for a problem body and in the catch before the rethrow. So the 19 hand-written
  `HandleError` overrides stay as they are, and no exit path leaves the slot at `Running` except
  two named, accepted coding-bug paths.
- **Option B hook rule:** the required `On…Loaded` seed hook is declared on every generated fetch
  (up to 16 empty bodies SPA-wide). The opt-in alternative leaves 4f to a test.
- **Forms and validation:** nothing to generate. The form already binds the contract's
  `IRoleDetails` and uses the contract's validator.
- **Steve's question** is in section 7. No option is picked and no implementation tasks are split.

### Review

- Round 1 ran at effort 2 with a general reviewer.
- Counts: bug 0. Suggestion 2 open (M1, M2). Nit 2 open (M3, M4).
- Fix pass 1 (2026-11-05) addressed all four in `proposal.md` and `task.md`:
  - M1: Option D now names a base-class `OnProblem` virtual for `Failed`, and the A-only check
    covers all three assignments. The scope line names the base change.
  - M2: Option B states the required-on-every-fetch rule and its cost, and the opt-in alternative
    with its reliance on the test.
  - M3: the count is 5 of 41, with the grep and why its sixth hit does not count.
  - M4: the Description and Checklist point at `proposal.md`, the split item is now "Steve answers
    section 7", and the 461 path is `kanban/in-progress/`.
- Round 2 (2026-11-05) ran at effort 2 with a general reviewer, on the fix delta
  `85dfe0f4e..29d388aff`. It re-verified M1–M4 as fixed and re-ran their greps. It raised one
  finding:
  - M5 (suggestion, open): in Option D, a transport exception, a validator throw, or an A-only
    `GetRequest` returning null skips `OnProblem`. The slot then stays at `Running`.
- Counts after round 2: bug 0. Suggestion 1 open (M5), 2 fixed. Nit 2 fixed.
- Fix pass 2 (2026-11-05) addressed M5 in Option D of `proposal.md`:
  - A table lists every path out of `ApiHandler.Handle` and the hook that closes it: null skip,
    success, problem body, transport exception, validation exception, and a throw in `GetRequest`.
  - Remedy picked: `Running` moves to a new `OnStarted(TRequest)` virtual, which runs after the
    null skip. The existing catch calls `OnProblem` with a synthetic problem before `throw;`, so
    validation failures still throw. The rejected remedies are named with reasons.
  - Accepted gaps are named: `DefaultApiHandler.HandleFileResponse` throwing, and a hand-written
    `HandleSuccess` throwing before its write.
  - The scope line and the A-only check now match. The check also flags `Running` set in
    `GetRequest`. The B example sets `Running` in an `OnStarted` override.
- Disposition is pending until review round 3.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/round-2/general.md`, `review/round-2/merged.md`.

### How to validate

Smoke: open `kanban/in-progress/464-*/proposal.md`. Check each section 4 fact against the cited
path:

- `git grep -n "\[Destructive" -- source` finds no uses.
- `git grep -l "DefaultApiHandler<" -- source/container-apps/web/projects/web-spa | wc -l` prints 42
  (41 handlers plus `default-api-handler.cs`).
- `git grep -n "UpdateRole\|DeleteRole" -- source/container-apps/web/projects/web-spa` finds
  nothing outside comments.
- `sed -n 55,70p source/container-apps/web/projects/web-spa/services/mocks/mock-web-api-service.cs`
  shows the Debug fallback.
- `git grep -l "action.Command" -- source/container-apps/web/projects/web-spa/features` lists 6
  files; 5 contain `DefaultApiHandler<` (the chat-state one does not).
- `git grep -l "override.*HandleError" -- source/container-apps/web/projects/web-spa/features | wc -l`
  prints 19.
- `sed -n 56,98p source/container-apps/web/projects/web-spa/features/base/api-handler.cs` shows
  the null skip at `:71` and the rethrow at `:87-92`, the paths in the Option D table.

Expect: every claim in sections 2 and 4 matches the code. Every option in section 5 is a code
document, with input, generated code, and hand-written hooks. Required hooks carry an access
modifier. Section 7 asks the question and does not pick.

## Review

- Round 1: Blocked — open M1, M2, M3, M4
- Round 2: Blocked — open M5
## Notes

- Related: 461 (back-end counterpart). Draft produced read-only from origin/HEAD `f003919db`.
