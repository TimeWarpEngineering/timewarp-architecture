# Round 2 — general
**Date:** 2026-10-30
**Scope reviewed:** fix-pass commit 92906f076 (152c2f866..HEAD, product paths), re-verifying round-1 M1–M5

## Summary
All five round-1 findings are fixed. The fix introduces no bug and no analyzer or compile risk that I could find by reading; the build and test run stay with the orchestrator. Three nits remain, all optional: one Design-region sentence that overstates by a hair, one out-of-order `using` pair, and a now-unused `internal` surface on `WriteTextAsync`.

## Prior findings
### M1 — Severity: suggestion — Status: fixed
- Evidence: `assembly-info-text.cs:17-19` adds `Title(AssemblyInfo?)`; `:52-61` adds `Links(AssemblyInfo)` returning `(Name, Url, DisplayText)`; `:63-72` `ClipboardText` is `NonBlank((Title, Title(assemblyInfo))).Concat(Properties).Concat(Links.Select(l => (l.Name, l.Url)))`. `AssemblyInfoModal.razor:25` renders the heading from `AssemblyInfoText.Title(AssemblyInfo) ?? "Assembly"`, `:28-31` the rows from `Properties`, `:32-35` the `LinkDisplay` rows from `Links`. No `AssemblyInfo.CommitUrl` / `RepositoryUrl` / `Title` is read outside `AssemblyInfoText` in the modal any more, so adding a link in `Links` shows and copies it. The two link display strings ("View Commit Details", "View Repository") now live in `Links` too. Order is Title, Properties, Links in both the markup and `ClipboardText`; the order itself is written twice (see Issue 1 for the wording of the Design claim). Blank test is the same `IsNullOrWhiteSpace` as `PropertyDisplay.razor` / `LinkDisplay.razor`.

### M2 — Severity: suggestion — Status: fixed
- Evidence: `assembly-info-copy-tests.cs` adds `Render_Copy_Beside_One_Close_In_The_Open_Dialog` and `Close_The_Dialog_From_Its_Own_Close_Button`, both through the nested `ModalHost` (`ModalController` + `AssemblyInfoModal`).
  - `ModalHost` renders once with `ActiveModalId == null`, so `ModalContainer.OnInitialized` has run `Parent.AddModal` (`ModalContainer.razor`). `OpenAsync` then changes `ActiveModalId` and `ModalController.OnParametersSetAsync` (`ModalController.razor`) finds the child in `ChildModals` and calls `modal.OnActivate`, which runs `AssemblyInfoModal.Activate` and loads `typeof(Program).Assembly.GetAssemblyInfo()`. So `OnActivate` does load AssemblyInfo in this harness, which is exactly what the round-1 `RenderShellModalsAsync` shape (set `ActiveModalId` on the first render, before any child registered) would not have done. The Design region says this ("first renders with no active modal and then opens").
  - Test 1 asserts exactly one `ModalButton` (a dropped `ActionContent` keeps the container default Close but then Copy is missing; a dropped re-declared Close gives 0), exactly one `AssemblyInfoCopy`, Copy index before Close index, and `>Version:<`. `PropertyDisplay.razor` renders `<span ...>@PropertyName:</span>`, so `>Version:<` is the real markup; the real Web.Spa assembly always has a Version.
  - Test 2 sends `SetActiveModal` to the store before rendering. That is not dead: `ModalHost` keeps its own `ActiveModalId` and never reads the store, so the pre-send is what makes `ActiveModalId` non-null in the store; without it `ShouldBeNull` after the click would pass vacuously. Then `ClickAsync("ModalButton")` runs the dialog's re-declared Close (`AssemblyInfoModal.razor:41`, `ApplicationState.CloseModal`).
  - Signatures exist as used: `ClickRenderer(IServiceProvider)`, `ClickHandlers` (Dictionary<string, ulong>), `RenderAsync<T>(IDictionary<string, object?>?)`, `ClickAsync(string)` (`notification-copy-test-doubles.cs`); `SpaTestScope.Send<TRequest>`, `.Store`, `.ServiceProvider` (`base-test.cs:73-111`). `ModalHost` is a private nested `ComponentBase` with a public parameterless constructor, so `InstantiateComponent` / `RenderComponentAsync<ModalHost>` work. `host!` is non-null: `Registered` fires in `OnInitialized`, which completes before `RenderAsync` / `RenderComponentAsync` return.
  - Design region now accurate; the false "ModalContainer needs a ModalController cascade" excuse is gone. The command-palette precedent is named for the HtmlRenderer shape, and the region states the one deliberate difference (open after the first render).

### M3 — Severity: nit — Status: fixed
- Evidence: `clipboard-js-module.cs:25` `DeniedBody`; `:27-32` `WriteResult { Written, Denied, Cancelled }`; `:35-51` `TryWriteTextAsync`. Used by all three handlers: `agent-links-state.copy-onboarding.cs:54-68`, `application-state.copy-assembly-info.cs:52-68`, `notification-state.copy-message.cs:51-62`. Each switch covers all three members. Grep over tracked source and tests (excluding `artifacts/`, `kanban/`, `obj/`): "blocked clipboard access" appears only at `clipboard-js-module.cs:25`; `JSException ||` appears only at `clipboard-js-module.cs:43`; no `CopyFailedBody` / `AssemblyInfoCopyFailedBody` reference is left in source or tests (the three tests that read the body now use `ClipboardJsModule.DeniedBody`). Semantics equal the removed code: same filter (`JSException` or `OperationCanceledException` with a live token) maps to Denied, the bare `OperationCanceledException` catch maps to Cancelled, and each handler still does what its old `catch` body did (the old swallow is now an empty `Cancelled` case with the same comment). The `ArgumentNullException` guards in `WriteTextAsync` throw inside an async method, so they still propagate uncaught as before. Removed `JSException` references leave no redundant using: `Microsoft.JSInterop` is a global using (`global-usings.cs:34`) and `IJSRuntime` is still used in all three files.

### M4 — Severity: nit — Status: fixed
- Evidence: `assembly-info-copy-tests.cs` adds `CommitDate = "2026-10-28T12:34:56"` via a new `Metadata(key, value)` helper (also used for `RepositoryUrl`), and `ExpectedText` gets `CommitDate: 2026-10-28 12:34:56` between `ShortHash` and `CommitUrl`. That matches `AssemblyInfoText.Properties` order (`ShortHash` is followed by `CommitDate` at `assembly-info-text.cs:44`, the last property) and then `Links` (`CommitUrl`, `RepositoryUrl`). The format is `yyyy-MM-dd HH:mm:ss` with `CultureInfo.InvariantCulture`, so the ISO input prints as expected in any culture. The ISO string parses with `DateTime.TryParse` in any culture.

### M5 — Severity: nit — Status: fixed
- Evidence: `using TimeWarp.State.Plus.Extensions;` is gone from `assembly-info-text.cs` and from `assembly-info-copy-tests.cs`; the test project's `global-usings.cs:29` still supplies it. The added test usings are not global in the test project (`global-usings.cs`, ImplicitUsings only adds System / IO / Linq / Collections.Generic / Net.Http / Threading / Tasks): `Microsoft.AspNetCore.Components` (ComponentBase, Parameter, ParameterView), `.Rendering` (RenderTreeBuilder), `.Web` (HtmlRenderer), `.Web.HtmlRendering` (HtmlRootComponent; confirmed present in the net11 ref XML), `Microsoft.Extensions.Logging` (ILoggerFactory). Each is used, so no new IDE0005. Only one ordering nit among them (Issue 2).

## New issues
### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/assembly-info/assembly-info-text.cs:7
- Description: The Design region opens "Everything the dialog shows comes from here". Two things the dialog shows do not: the `"Assembly"` fallback heading (`AssemblyInfoModal.razor:25`) and the loading spinner (`:37-40`); and the Title, Properties, Links order is written once in the Razor markup and once in `ClipboardText`, so the order, unlike the members, still agrees by convention. Neither affects the clipboard, because Copy renders only after the record loads, so the claim is true for everything the clipboard cares about.
- Suggestion: Narrow the sentence, for example "Every value the dialog shows comes from here (the loading state and the fallback heading aside)". Optional; nothing in the code needs to change.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/assembly-info-copy-tests.cs:23-24
- Description: `using Microsoft.AspNetCore.Components.Web.HtmlRendering;` comes before `using Microsoft.AspNetCore.Components.Web;`; every other `using` in the file (and in `message-bar-copy-tests.cs`, `command-palette-tests.cs`) is in ordinal order. No analyzer enforces ordering here (`dotnet_sort_system_directives_first = false`, no IDE0065 sort rule), so this is cosmetic only.
- Suggestion: Swap the two lines.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/services/clipboard-js-module.cs:53
- Description: `WriteTextAsync` is still `internal`, but its only caller is now `TryWriteTextAsync` (no other reference in source or tests). Leaving it internal lets a future handler call it directly and re-grow the try/catch copy the Design region says `TryWriteTextAsync` replaced ("the one place the copy handlers classify a failure"). `TryWriteTextAsync` itself being `internal` is consistent with the class.
- Suggestion: Make `WriteTextAsync` `private`. Optional.
- Status: open

## Verified claims (no issue)
- `AssemblyInfoText.Title`: `string.IsNullOrWhiteSpace(assemblyInfo?.Title) ? null : assemblyInfo.Title` is null-safe. `IsNullOrWhiteSpace` is `[NotNullWhen(false)]`, so the false branch knows both `assemblyInfo` and `.Title` are non-null, and the return type is `string?` anyway. A whitespace-only title now shows `"Assembly"` instead of a blank heading, which is consistent with the clipboard dropping it.
- Razor `@foreach ((string _, string url, string displayText) in ...)`: the typed `_` is a discard in a deconstruction (C# 9+), so `Name` is skipped without an unused-variable warning; same shape as the existing `(string name, string value)` loop. Compilation is the orchestrator's gate.
- `Links` / `NonBlank` use the same filter-then-`!` shape; `[.. ...]` into `IReadOnlyList` is valid.
- Test double flow: the second render's `ModalController` parameter set loads AssemblyInfo and the modal is inserted active in one render batch, so `ClickRenderer.ClickHandlers["ModalButton"]` is recorded from a full element frame, not a stale diff.
- `Occurrences` (`text.Split(token).Length - 1`) is correct for the non-empty literal tokens used.
- `TryWriteTextAsync` does not change what the Cancelled / Denied paths do for `NotificationState.WriteAndConfirmAsync`, which `CopyMessageBar` also uses (shared helper), so both regressions classes keep one behavior.
- Design-region width: several of the rewrapped regions have long or ragged lines (`agent-links-state.copy-onboarding.cs:7-8` is 101 and 150 columns). The repo sets no line limit and other files have longer lines (for example `agent-conversation-credential-issuer.cs:10` at 155), so I did not raise it.
- Not run (read-only brief): build, tests, and `ganda repo audit`; the orchestrator owns those.
