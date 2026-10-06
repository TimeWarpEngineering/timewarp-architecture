# Replace argument-carrying server offers with server-computed availability flags and page-scoped actions

## Description

Tasks 275 → 279 → 280 → 281 built "server offers": the server lists catalog actions **plus
pre-filled arguments** for every item, for example "Revoke credential A", "Revoke credential B",
and the client runs them through the catalog, Ctrl-K contextual rows, a JSON → args binder,
typed offer records, an `[Offerable]` generator and analyzers TWA0029–0031.

Steve's review on 2026-10-06 concluded this **doesn't scale and adds clutter**: offers enumerate
actions × items, flood Ctrl-K, and need a hand-written composition (`CredentialOffers.For`) plus
string-agreement machinery. The part that mattered, **the server owning availability** so the
client never copies business rules, is kept with a simpler model.

### Decided model (Steve, 2026-10-06)

1. **The server owns availability through typed flags.** Each item in a list carries typed
   permission flags, for example `CredentialSummary.CanRevoke` and `CanRename`. Page-level
   actions get a flag on the page's response, for example `GetCredentials.Response.CanLinkMicrosoft365`.
   All of them come from `CredentialRules`. **Typed flags, not an allowed-names list.**
2. **Item actions live on the page.** Row buttons call their action directly with the row's item:
   `RevokeCredential(id)`, `RenameCredential(id, nickname)`. Typed and checked by the compiler;
   no strings and no name contract.
3. **Parameters that need typing get a form on the page,** such as Rename's nickname. There is no
   generic form system.
4. **Ctrl-K and other global entry points get navigation plus parameter-free actions only.**
   "Revoke a passkey" means going to the Passkeys page. **The Ctrl-K context hook is removed
   entirely.**
5. **Agents get tools scoped to the current page** (WebMCP): future work under task 271, not here.

Revoke and Rename `[CatalogAction]` **Visibility goes back to `Agent`**. 279 changed them to
`Both` only so its offer permission check would accept them, and that reason disappears.

## Keep

- **`CredentialRules`** (`credential-rules-application.cs`: `CanRevoke`, `HoldsMicrosoft365`,
  `CanLinkMicrosoft365`). They are enforcement: `RevokeCredential.Handler` (~L131) and
  `EntraTicketProcessor` (~L457) use them. Only how their results reach the client changes.
- The action catalog, and Ctrl-K's normal roster (`CommandPaletteRoster`: pages plus parameter-free
  Human/Both actions, with its own Visibility and Permissions checks).
- Everything from 260/265/278 (interactions are actions, TWA0026, the JS dispatch allow-list).

## Change

| Where | Today | After |
|---|---|---|
| `get-credentials-contracts.cs` | `Response.Offers` (`IReadOnlyList<OfferedAction>`, ~L96); mock factory builds offers (~L195–198) | `CredentialSummary.CanRevoke` / `CanRename`; `Response.CanLinkMicrosoft365`; update the mock factory and round-trips |
| `get-credentials-handler-application.cs` (~L88) | `new Response(summaries, CredentialOffers.For(...))` | set the flags from `CredentialRules` |
| `credentials-state.cs` / `credentials-state.fetch-credentials.cs` | `OffersList`, `Offers`, `FindOffer`, `IsOffered(name, id)`, `CredentialOffer.From` mapping | summaries carry the flags; no offer state |
| `SettingsPage.razor(.cs)`, `PasskeysPage.razor`, `CredentialList.razor` | `IsOffered` + `RunOfferAsync` / `CredentialOfferRows.RunAsync`; `IsRevokeOffered` / `IsRenameOffered` callbacks; `[CrossSliceReference(typeof(CredentialOfferRows))]` | read the flags; buttons dispatch `RevokeCredential(id)` / `RenameCredential(id, nickname)` directly; remove the cross-slice reference |
| `[CatalogAction]` Revoke / Rename (`credentials-state.revoke-credential.cs`, `.rename-credential.cs`) | `Name = OfferName`, `Visibility = Both` | default derived name, `Visibility = Agent` |
| `[CatalogAction]` Link Microsoft 365 | `Name = OfferedActionNames.LinkMicrosoft365` | default name; keep `DisplayName = "Link Microsoft 365"`, Visibility Human. It stays a general Ctrl-K command, and the sign-in flow reports when it doesn't apply |
| `skills/tw-blazor/SKILL.md` "Server-offered actions" (~L91–209) | offers pattern | replace with a short **"Server-owned availability"** section stating rules 1–5 above (public: rules plus reasoning) |
| `skills/tw-web-api-contracts/SKILL.md` `[Offerable]` (~L132–171) | offers section | remove |
| Code comments citing 275/279/280/281 (for example `entra-ticket-processor-application.cs:28`, `revoke-credential-handler-application.cs:49`) | describe offers | reconcile with the new model |

## Remove

**Server and contracts**
- `features/identity/offered-action-contracts.cs` (`OfferedAction`, `OfferedActionNames`)
- `features/identity/credential-action-offer-contracts.cs` (`ICredentialActionOffer`, `LinkMicrosoft365Offer`)
- `features/identity/credential-offers-application.cs` (`CredentialOffers`)
- `[Offerable]` and the `partial record Offer` on `RevokeCredential` / `RenameCredential` contracts

**Attributes, generator, analyzers**
- `source/analyzers/timewarp-architecture-attributes/action-offer-attribute.cs`, `offerable-attribute.cs`
- `source/foundation/foundation-contracts-generators/contracts-generator.offerable.cs` and its call
  in `contracts-generator.cs` (~L88)
- In `foundation-contracts-generators.csproj`: the `RS2008` NoWarn and the linked
  diagnostics-descriptor SSOT include added only for TWE012–014. **Restore the project's previous
  shape**
- TWE012/013/014 descriptors. Mark them **retired/reserved** in the SSOT comment and the AGENTS.md
  "Retired / reserved generator IDs" line, the same way as TWE001/004
- `source/analyzers/timewarp-architecture-convention-analyzers/action-offer-agreement-analyzer.cs`
  (TWA0029/0030/0031). Mark them **retired/reserved** in the AGENTS.md TWA table, the same way as
  TWA0005, and update the Analyzers package row range
- The `AnalyzerReleases.Unshipped.md` entries for the retired ids (handle per Roslyn release
  tracking for unshipped removals)

**SPA**
- `features/application/command-palette/contextual-action-arguments.cs` (binder)
- `command-palette-context.cs` (including the offer permission check), `i-command-palette-context-source.cs`
- In `command-palette-row.cs`: the `Contextual` row kind and `RequiresInput`. In `command-palette-runner.cs`:
  the contextual path. In `command-palette-state.open.cs`: the contextual merge (~L49–57). In
  `CommandPalette.razor`: the context injection and pass-through
- `features/identity/credentials-context-source.cs`, `credential-offer-rows.cs`, `credential-offer.cs`
- In `program.cs`: `AddScoped<CommandPaletteContext>()` and
  `AddScoped<ICommandPaletteContextSource, CredentialsContextSource>()` (~L164–165)

**Tests (replace, don't just delete)**
- `tests/analyzers/timewarp-architecture-analyzers-tests/action-offer-agreement-analyzer-tests.cs`
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-offerable-tests.cs`
- `tests/container-apps/web/web-server-integration-tests/features/identity/credential-offers-tests.cs`
- `tests/container-apps/web/web-spa-integration-tests/features/identity/credential-offers-tests.cs`
- the offer hooks (`ExtraOffers` / `SuppressOffer`) in `credentials-spa-test-application.cs`
- the offer parts of `web-contracts-tests/.../identity-contracts-serialization-tests.cs`
- the `[ActionOffer]` Attributes `Using` in `web-spa-integration-tests.csproj` (~L44), if nothing
  else needs it

## Requirements

1. Implement the **Change** table and the **Remove** list. Leave no dead references; `dev build`
   must stay 0/0.
2. **New tests:**
   - the server flags match the rules: one active credential means `CanRevoke` false; two means
     true; `CanRename` is true for active credentials; `CanLinkMicrosoft365` is true only when the
     site offers it and the account isn't linked;
   - Settings/Passkeys buttons show and hide from the flags;
   - Revoke/Rename dispatch the real actions;
   - the server still refuses a stale Revoke with 409 regardless of what the client shows;
   - Ctrl-K on Settings/Passkeys shows only the normal roster (no contextual rows);
   - flag round-trips in the contract serialization tests.
3. Retired ids stay reserved and are never reused. Record why in the SSOT comments.
4. Reconcile the Purpose/Design regions on every touched file. No Design region should describe
   offers afterwards.
5. Update the task 271 note, if it refers to offers as the agent foundation, so it points to
   page-scoped tools instead.

## Checklist

- [x] Typed flags on `CredentialSummary` and `GetCredentials.Response`, from `CredentialRules`
- [x] Pages and `CredentialList` use the flags and dispatch actions directly
- [x] Revoke/Rename `[CatalogAction]`: default names, `Visibility = Agent`; Link M365 default name
- [x] Ctrl-K context hook removed entirely (context, sources, row kind, runner path, Open merge, DI)
- [x] Offers machinery removed (OfferedAction, offer records, `[ActionOffer]`, `[Offerable]`, generator part, binder)
- [x] TWE012–014 and TWA0029–0031 retired/reserved; release notes, AGENTS.md and the package row updated
- [x] Generator project's RS2008 suppression and its linked include removed
- [x] Skills: `tw-blazor` "Server-owned availability" replaces "Server-offered actions"; the `tw-web-api-contracts` offers section is removed
- [x] Tests replaced per Requirement 2
- [x] Gates: `dev build` 0/0 (analyzer/generator change means a full rebuild), `dev test`,
      `dev template-smoke`, `ganda repo audit`, `dev check-version` (analyzers/generators/attributes
      packages change)
- [x] Do **not** start an AppHost; record the browser check as not performed
- [x] Implementation review (clean, 1 round); host `open-pr`

## Notes

- Origin: design discussion with Steve, 2026-10-06 (cockpit). The inventory of current master
  came from a read-only sweep the same day.
- Approach C (link-following) stays rejected, as recorded in 275.
- The server-driven state ideas (Blazor Server + TimeWarp.State; "C + store slices") remain
  possible future timewarp-state questions and are out of scope here.
- The TimeWarp.State action catalog itself is unaffected: it still serves Ctrl-K's normal roster
  and future agent tools.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Session

- Created: 2026-10-06 (cockpit, per Steve)
- 2026-10-06 implement oracle (implementer-claude, headless): Change table + Remove list done, tests
  replaced, gates green (see Results). No AppHost started.
- 2026-10-06 review oracle (claude-opus-5-5, headless): tw-implementation-review, effort 3, roster
  general (Claude subagent). Disposition clean.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-06T04:02:08Z

## Results

**Model now:** the server owns availability through typed flags on `GetCredentials`:
`CredentialSummary.CanRevoke` / `CanRename` (per row, default `false`) and
`Response.CanLinkMicrosoft365`. The handler sets them from `CredentialRules` over the caller's
**active** credentials, whatever `IncludeRevoked` asks for. Revoked rows get both flags `false`.
Settings, Passkeys and `CredentialList` read the flags. The buttons dispatch
`RevokeCredential(id)` / `RenameCredential(id, nickname)` directly and then `FetchCredentials`,
which is the pre-279 sequencing. Ctrl-K is back to the plain roster.

**Removed:** `OfferedAction` / `OfferedActionNames`, `ICredentialActionOffer` /
`LinkMicrosoft365Offer`, `CredentialOffers`, `[Offerable]` + the partial `Offer` records,
`ActionOfferAttribute` / `OfferableAttribute`, `contracts-generator.offerable.cs` (generator csproj
and `contracts-generator.cs` restored to their pre-281 shape, including the RS2008 NoWarn and the
linked descriptor include), `ActionOfferAgreementAnalyzer`, and the SPA Ctrl-K context hook
(`CommandPaletteContext`, `ICommandPaletteContextSource`, `ContextualActionArguments`, the
`Contextual` row kind / `RequiresInput`, the runner's contextual path, the Open merge, the
`CommandPalette` injection, `CredentialsContextSource`, `CredentialOfferRows`, `CredentialOffer`,
and both DI lines). `command-palette-row/runner/open` and `-ranker` comments match pre-275 again.

**Catalog:** Revoke / Rename use default names (`Credentials.RevokeCredential` /
`Credentials.RenameCredential`) with `Visibility = Agent`. Link Microsoft 365 uses its default name
`Credentials.LinkMicrosoft365` and keeps `DisplayName` and Visibility Human. `FetchCredentials` stays
cataloged as Agent, so an agent can read the list and its flags.

**Retired IDs:** TWE012/013/014 (SSOT comment in `diagnostic-descriptors.cs` plus the AGENTS.md
retired line) and TWA0029/0030/0031 (one retired row in the AGENTS.md table). The package row,
`Directory.Build.props` comment and analyzers csproj description now read TWA0020–0028. None of
these IDs was ever in a release (v2.0.0-beta.19 has no offers code), so per Roslyn release tracking
their `AnalyzerReleases.Unshipped.md` lines were deleted, not moved to a Removed section.

**Skills:** `tw-blazor` "Server-offered actions" became a short "Server-owned availability"
section with rules 1–5 and their reasons. The `tw-web-api-contracts` `[Offerable]` section and its
checklist line are gone.

**Tests:**
- `web-server-integration-tests/.../credential-availability-tests.cs` (renamed from
  credential-offers): the host-free `CredentialRules_` table, plus real HTTP flags. One credential
  gives `CanRevoke` false and `CanRename` true; two give both true; revoked rows get no flags and the
  remaining row drops `CanRevoke`; `CanLinkMicrosoft365` is true only when Entra is offered and not
  linked. `Refuse_A_Stale_Revoke_With_409` acts twice on an old snapshot where both rows said
  `CanRevoke`.
- `web-spa-integration-tests/.../credential-availability-tests.cs` (replaces credential-offers):
  the state holds the server's flags and follows them when they contradict the count; Revoke and
  Rename dispatch the real actions and refresh the flags; Ctrl-K on /Settings and /Passkeys returns
  exactly the /Counter roster (no per-credential rows, Link listed once). The scripted BFF sets its
  flags with the real `CredentialRules`; the `ExtraOffers` / `SuppressOffer` hooks were replaced by
  `CanRevokeOverride`.
- `credential-list-render-tests`: `CanRevoke` false disables the button and shows the hint;
  `CanRevoke` true shows no hint; `CanRename` false disables Rename. The prerender facts in
  `protected-page-deep-link-tests` (Settings and Passkeys, Revoke/Unlink/Link) already prove the
  handler's flags reach the rendered buttons; only their comment changed.
- Contracts round-trips: flags per row and page-level, the mock factory's flags on the wire, and
  missing flags deserialize to `false`. The `OfferedAction_Should` class was removed.
- `action-catalog-tests` / `command-palette-tests` are back to the `Credentials.*` names. The
  catalog test also pins Revoke and Rename as Agent-only. The `[ActionOffer]` `Using` was removed
  from the SPA test csproj.
- Task 271 got a note pointing agent work at page-scoped tools. It never referred to offers.

**Gates (2026-10-06, this worktree):**
- `dev clean` and full `dev build`: 0 warnings, 0 errors.
- `dev test`: 21/21 suites passed (web-server-integration 287, web-spa-integration 137, web-jaribu
  227, contracts 48, …).
- `dev template-smoke`: SUCCEEDED.
- `dev check-version`: 2.0.0-beta.20 is new against 2.0.0-beta.19.
- `ganda repo audit`: passes all checks.

**Not performed:** browser check (no AppHost started, per the checklist).

### How to validate

**Smoke** (no AppHost):

```bash
dev build                       # expect 0 warnings / 0 errors
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class CredentialAvailability
cd ../web-spa-integration-tests && dotnet test -c Release -- --filter-class CredentialAvailability
cd ../web-contracts-tests && dotnet test -c Release -- --filter-class GetCredentials_Response
git grep -nw "OfferedAction\|ActionOffer\|Offerable\|CommandPaletteContext\|RunContextualAsync" -- source tests skills
```

**Expect:** the build is 0/0. The suites pass: server 10/10 (3 rule-table plus 7 HTTP, including
`Refuse_A_Stale_Revoke_With_409`), SPA 6/6, contracts 4/4. The final `git grep` matches only the
retired-ID comment in `diagnostic-descriptors.cs` (`[Offerable]` contract of the server-offers
model…).

Maintainer, after merge (`dev clean`, `dev run`, clear site data):
1. With one credential, Settings shows Revoke disabled with its hint ("Add another passkey or agent
   key before revoking this one.").
2. Add a passkey and Revoke becomes enabled; revoke one and it is disabled again.
3. Rename works through its nickname field.
4. Ctrl-K on Settings shows only pages and general commands ("Credentials: Link Microsoft 365"
   included when signed in), with no per-credential rows.

### Implementation review

- **Rounds:** 1 · **Effort:** 3 (by-diff, 5724 lines) · **Roster:** general
- **Final counts:** bug 0 / suggestion 0 / nit 0 (0 open, 0 fixed, 0 wontfix)
- **Disposition:** clean, with no exceptions and no escalations
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/disposition.md`
