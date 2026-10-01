# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** git diff master...HEAD (commit 219fe923): ComponentSideEffectAnalyzer (TWA0026/0027), both attributes, web-spa action/handler additions (RedirectToLogin, ForgetPasskeySoftPromptLater), RedirectToLogin/LoginPage/AuthenticationStateListener/CounterPage/CommandPalette razor changes, counter.ts JS dispatch, Purpose/Design regions, skill/AGENTS text. Static read only; no build or test run.

## Summary
The analyzer logic is sound for the cases traced: nearest-type component check, lambda/local-function and accessor opt-out walk-up, merged partial-class attributes, reduced/unreduced extension methods, method-group references, and razor-tree gating. The JS dispatch action name matches the real nested action type and the Web.Spa assembly name, and no template conditional regions were touched. The only real concern is cost: the side-effect test runs on every invocation in the SPA compilation, before the cheap component check, using string-based symbol comparison. The rest are minor false-negative edges.

## Issues
### Issue 1 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/component-side-effect-analyzer.cs:173
- Description: Analyze() runs IsSideEffect before IsInComponent. IsSideEffect calls ToDisplayString() (string allocation) up the BaseType chain and across AllInterfaces for NavigationManager, IJSRuntime, IJSObjectReference, IApiService, HttpClient and 4 storage names, then walks GetAttributes on the containing type, receiver type and all interfaces. This runs for every invocation and method reference in every tree of the SPA compilation (including all razor-generated code), almost all of which are not in a component or not side effects. Types are also matched by display string rather than resolved once per compilation.
- Suggestion: Check IsInComponent (and the generated-code exemption) first, and resolve the framework INamedTypeSymbols once in RegisterCompilationStartAction (GetTypeByMetadataName + SymbolEqualityComparer, AllInterfaces/BaseType comparison). Optionally cache the per-receiver-type verdict in a ConcurrentDictionary<ITypeSymbol, bool> in the compilation-start scope.
- Status: open

### Issue 2 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/component-side-effect-analyzer.cs (GetReceiverType / IsSideEffect marker check)
- Description: Receiver is method.ContainingType for instance calls, so a [SideEffectService] class that inherits a member from an unmarked base (e.g. a helper defined on a base class) is not flagged when called through the marked derived type; the marker/Design text says "a type that implements / derives from one" for the reverse direction only. Also an unreduced extension whose `this` parameter is a type parameter (`this T`) yields a null receiver and is skipped.
- Suggestion: Where the operation has an instance, also test the static type of invocation.Instance for the marker, or document the limitation in the Design region. Low impact today (all marked services declare their own members).
- Status: open

### Issue 3 — Severity: nit
- File: source/analyzers/timewarp-architecture-attributes/direct-component-side-effect-attribute.cs:20
- Description: AttributeUsage allows Class, Method, Property, Constructor only. A side effect in a field initializer (operation ContainingSymbol is the field) can only be opted out at class level, and the analyzer Design region lists "method, property, constructor" consistently, so this is a known narrowing, but the TWA0027 symbol action also skips fields/events.
- Suggestion: Acceptable as is; consider adding AttributeTargets.Field if a field-initializer case ever needs a member-level opt-out.
- Status: open
