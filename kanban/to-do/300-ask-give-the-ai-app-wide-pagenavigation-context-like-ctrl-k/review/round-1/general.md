# Round 1 — general
**Date:** 2026-10-15
**Scope reviewed:** branch vs master (5df7913d2)

## Summary
The change does what the spec asks. NavMenu, Ctrl-K, `navigate` and `site_map` all read `NavigationDestinations`, and the hand-written route map and the per-route `page_context` cases are gone. `navigate` is approval-wrapped in Ask and gated in WebMCP, and it refuses unknown urls before any prompt. The NavMenu `#if` regions could be removed safely: `.template.config/template.json` excludes `features/weather-forecast/**` (`!api`) and `features/superhero/**` (`!grpc`), so those pages drop out of the generated registry, and no test hard-codes their urls. Build and the smoke tests pass. No correctness bugs found. The findings are test-strength gaps, guard holes, one silent-default in the generator, and small nits.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:137
- Description: `AssertSameDestinationsAsync` checks that the menu, palette, `navigate` enum and `site_map` all match `NavigationDestinations.ForAsync`. It never checks what that function should return for each principal. Its only fixed assertion is `destinations.ShouldContain("/")`. If `NavigationDestinations.IsAuthorizedAsync` returned true for everything, all four principal tests would still pass: the surfaces would agree and the `refused` list would be empty. The expected sets (Anonymous `/`, PlainUser `/,/Settings,/Profile,/AgentLinks,/Feedback`) are only printed as `DESTINATIONS-PROOF`. The checklist item "Unit tests: policy filtering (admin / plain user / developer / anonymous)" is met only indirectly, through the older command-palette tests (`Offer_Anonymous_Visitors_Only_Unrestricted_Pages` and the denied-target list near line 220).
- Suggestion: Add per-principal assertions that still hold with template flags off. Anonymous equals `["/"]`. PlainUser equals the literal five urls. Developer contains no `/Admin/*` and not `/Settings`. Administrator equals `PageRegistry.All` urls.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:70
- Description: The guard is weaker than the requirement (`features/application/agent/**`):
  - (a) `Directory.GetFiles(agent, "*.cs")` is not recursive, so a future subfolder under `agent/` is never scanned.
  - (b) It only catches a string literal starting with `"/Letter"` or `typeof(...Page)`. A hand-written list such as `[SettingsPage.GetPageUrl(), HomePage.GetPageUrl()]`, `nameof(SettingsPage)`, or `PageRegistry.All.Where(p => p.Title == "Settings")` gets through.
- Suggestion: Use `SearchOption.AllDirectories`. Add patterns for `\b\w+Page\.(GetPageUrl|Title|NavIcon|Policy)\b` and `nameof\(\s*[\w.]*Page\s*\)`.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:290
- Description: `NavGroup` is read only as a `LiteralExpressionSyntax`. `NavGroup = MenuGroups.Admin` (a const) silently becomes `""`, and the page moves to the top level of the menu. `NavOrder` right below it accepts constants (`semanticModel.GetConstantValue`), and `Policy` accepts const expressions, so the rules are inconsistent. The Design comment (lines 53-56) says "the registry guard tests report it", but no test reports a non-literal `NavGroup`. The only registry check is strictly increasing `NavOrder`.
- Suggestion: Read `NavGroup` with `semanticModel.GetConstantValue` as `NavOrder` does, or report a TWE diagnostic for a non-constant value. Then fix the comment.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs:96
- Description: NavMenu (`NavMenu.razor:30-36`) starts a new `FluentNavCategory` every time `NavGroup` changes between consecutive entries. If a page's `NavOrder` lands inside another group's range (e.g. a new Admin page at `NavOrder = 215`), the menu renders a second "Admin" category. Nothing catches this. `Order_The_Registry_By_NavOrder_And_Name_Catalog_Actions` checks only that `NavOrder` strictly increases.
- Suggestion: In that test, assert that each non-empty `NavGroup` appears as one contiguous run in `PageRegistry.All`.
- Status: open

### Issue 5 — Severity: nit
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:29
- Description: A stale Design line still says registry entries are "Sorted by route (ordinal) for stable output". `PageRegistry.All` is now sorted by `NavOrder`, then route (lines 52-53 and the emitted summary).
- Suggestion: Change it to "Sorted by NavOrder, then route (ordinal)".
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-scope.cs:205
- Description: `TokenAccepts` fails open for any constraint it does not know. `datetime`, `decimal`, `double`, `float`, optional `guid?`, and composite `int:min(1)` all accept any segment, so a page's `AgentActions` would serve on paths Blazor itself would not route. Optional (`{id?}`) and catch-all (`{*rest}`) tokens never match because the segment counts must be equal. Today every parameterized `[Page]` uses `:Guid`, so nothing is affected yet.
- Suggestion: Treat an unknown constraint as no match, or add the remaining Blazor constraint names. Optionally, make the generator reject templates that the matcher cannot handle.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-scope.cs:265
- Description: `PrimaryScope` and `PrimaryPage` choose the off-page `navigateTo` route by menu rank and ignore the principal. Example: a principal with `credential.manage.self` but not `settings.read` gets `Credentials.AddPasskey` offered with `navigateTo` `/Settings`, which `navigate` then refuses. The new Preface now says both "call navigate with that url" and "Never navigate to a url that is not in the site map", so the model gets contradictory instructions. The menu-rank choice predates this change (insertion order), but this change adds the contradiction.
- Suggestion: Optionally pass the principal's destinations, prefer a permitted scope, and drop the offer when the action's pages are all outside the site map.
- Status: open

### Issue 8 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor:24
- Description: `OnParametersSetAsync` reruns `NavigationDestinations.ForAsync`, one `AuthorizeAsync` per registry page, every time `TimeWarpPage` re-renders. A `Task<AuthenticationState>` cascading parameter always counts as changed, so this runs on every parent render and not only on sign-in or sign-out as the Design comment says.
- Suggestion: Cache the `AuthenticationState` task reference and recompute only when it changes.
- Status: open

## Verification run
- `git log master..HEAD`: the local `master` ref is stale (behind origin), so `master...HEAD` also shows commits from tasks 362 and 363. I reviewed `HEAD~1..HEAD` (5df7913d2 only).
- `./bin/dev build`: Build succeeded, 0 Warning(s), 0 Error(s). (`dev` is not on PATH in this shell; I used the repo's `./bin/dev`.)
- `dotnet run --project tests/container-apps/web/web-spa-integration-tests -- --filter-class NavigationDestinations_`: 6/6 passed. The DESTINATIONS-PROOF lines match the Results: Anonymous `/`; PlainUser `/,/Settings,/Profile,/AgentLinks,/Feedback`; Developer shows Demos and Developer and no Settings; Administrator shows all 18.
- `dotnet run --project tests/analyzers/timewarp-architecture-sourcegenerator-tests -- --filter-class PageSourceGenerator`: 31/31 passed. The Results' 120/120 is the whole suite; I did not re-run that.
- `dotnet run --project tests/container-apps/web/web-server-integration-tests -- --filter-class XaiChatUpstream_`: 8/8 passed.
- Not run, per instructions (they need an AppHost): `CatalogAgent_Should`, `AskSignInPlaywright_`.
- Repo checks:
  - `TimeWarpNavLink` is referenced only in kanban history.
  - `ActionNamesFor` and `PagesFor` have no remaining references.
  - `PageAgentScope.KnownRoutes` is still used by `catalog-agent-parity-tests.cs:42`.
  - `template.json` excludes the weather-forecast and superhero features under `!api` and `!grpc`.
  - The `AgentActions` declarations on Counter, Settings, Passkeys, Profile, Admin/Roles/New, Admin/Authentication, Feedback and Feedback/{id} reproduce the deleted map one for one.
