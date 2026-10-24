# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** commit 6861cc5bd (source + tests), plus call sites: `SignInState`, `LoginPage`, `ChooseMicrosoft365Page`, `TimeWarpPage`, web-server `ConfigureServices` (calls `Web.Spa.Program.ConfigureServices`, so prerender binds `SignInOptions`).

## Summary

The change adds `SignInOptions` / `SignInMode` with a FluentValidation `IsInEnum` guard, moves the login card into `SignInChoices`, gates the add-passkey prompt and the required gate on `OffersPasskey`, and hides "I already have an account" outside Passkey mode. The checked-in default stays Passkey, so the default app is unchanged. Re-verified: `./bin/dev build` 0 errors; `SignInOptions_Should_`, `SignInChoices_Should_`, `TimeWarpPage_Should_` all pass. Template-smoke and audit were not re-run by this reviewer. The main defect is a load-time state: the new "not available" line keys off a `bool` that is `false` until the fetch returns.

Observation, not a finding: the server and WebAssembly hosts read separate `SignInOptions` sections. An operator who overrides only the server (for example `SignInOptions__Mode=Staff` as an environment variable) gets a WASM card that swaps back to Passkey after hydration. The task specifies the two sections, and `BrandOptions` uses the same pattern, so this is out of scope here.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/identity/components/SignInChoices.razor:51
- Description: `SignInState.Microsoft365Offered` is a plain `bool`. `Initialize()` sets it to `false` (sign-in-state.cs:34, :51). `LoginPage.OnInitializedAsync` awaits `FetchSession` and then `FetchMicrosoft365Offered`. Under InteractiveAuto, InteractiveServer, or WASM, the component renders at the first incomplete await, while the value is still `false`. `SignInState` is not persisted from prerender. In Microsoft365Only or Staff mode, the first interactive render replaces the prerendered primary "Continue with Microsoft 365" button with "Microsoft 365 sign-in is not available." The message is false and stays until the fetch completes. It also appears when the fetch itself fails. The goal says the card shows "not available" only when the site does not offer Microsoft 365.
- Suggestion: Make the offer tri-state (`bool? Microsoft365Offered`, `null` until fetched). Show the unavailable line only when the value is `false`, and show nothing (or the button disabled) while it is `null`. Add a render case with the offer still unknown.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor:54
- Description: The goal requires the choose-account page to stop offering "I already have an account" in either non-passkey mode, and the diff adds a Staff line and different copy there. No test pins this. `SignInChoices_Should_` covers only the login card, and no render test asserts that `AlreadyHaveAccount` is absent outside Passkey mode or that `ChooseStaffNote` appears in Staff mode.
- Suggestion: Add a render test (or move the unlinked-account body into a small component the way `SignInChoices` was split out) that pins Passkey (both buttons), Microsoft365Only (no `AlreadyHaveAccount`, no staff note), and Staff (`ChooseStaffNote`).
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/configuration/sign-in-mode.cs:8
- Description: The Design region says "The portal card that inspired this is a work-account button plus that line; the product name stays out of the template." This is provenance history in template source that ships to every generated app. A reader of a generated app has no "portal" to refer to. The Design region should state the rule, not where the idea came from.
- Suggestion: Drop that sentence. If the no-client-name rule should stay, state it as a rule ("Copy is generic; no product or client name"), as sign-in-options.cs already does.
- Status: open
