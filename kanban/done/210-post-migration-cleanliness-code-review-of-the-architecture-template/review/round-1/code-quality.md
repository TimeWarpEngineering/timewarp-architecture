# Round 1 — code-quality
**Date:** 2026-09-09
**Scope reviewed:** `source/` and `tools/dev-cli` for suppressions, TODO/HACK/FIXME/XXX, dead code (Roslynk `find_dead_code` on `timewarp-architecture.slnx` + manual scans), pattern conformance (FluentValidationBehavior-only validation, `ContractSerializationDefaults`, no MediatR, no hand-written `BaseEndpoint`, `[ApiEndpoint]` auth-marker reasons, TWA0022, no FluentAssertions, no Tailwind), Agent Context Region honesty (targeted fold-review files under `web-spa/features/identity/**`, `web/features/authorization/**`, `host-graph-factory.cs`, all three `program.cs` bootstraps, plus a 25-file sample across `features/`, `platform/`, `foundation/`), family (web/api/grpc) duplication, naming spot-checks, and `source/libraries/timewarp-402` / `timewarp-identity` template-exclusion safety. High-value claims from research passes were independently re-verified by reading the actual files, running a clean `dotnet build` where needed, and querying Roslynk directly rather than trusting a single pass.

## Summary
The template is in good shape: zero MediatR usings, zero FluentAssertions, zero Tailwind leftovers, zero direct `.Send()` calls in SPA code, zero hand-written `BaseEndpoint` shims, and every sampled Purpose/Design region (including all files touched by the SPA-identity fold, task 132-001) accurately describes the code beneath it — no stale-Design bugs found. The real findings are template-hygiene items: a few `<Pending>`-justification suppressions, a blanket unjustified `<NoWarn>` list on `container-apps`, an unregistered/dead demo pipeline behavior with a placeholder name, one dev-cli command whose output claims success without doing any work, one avoidable CORS duplication between `grpc-server` and the shared `foundation-server` `CorsPolicy`, and a couple of redundant/no-comment suppressions worth trimming.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/endpoints/verify-samples-command.cs:20-26
- Description: `VerifySamplesCommand.Handle` prints `"Verifying samples..."` then `"Samples verified successfully!"` around a bare `// TODO: Implement sample verification logic specific to this repo` — no verification runs at all. The command reports success unconditionally, which is misleading CLI output rather than an honest stub (an honest stub would say "not implemented" or exit non-zero).
- Suggestion: Either implement the verification, or change the output/exit code to make clear nothing was checked (e.g. `Terminal.WriteErrorLine("verify-samples not yet implemented"); Environment.ExitCode = 1;`) until task work lands.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/pipeline/my-behavior.cs:1-60
- Description: `MyBehavior<TRequest,TResponse>` is a generically-named ("MyBehavior") sample `IPipelineBehavior` kept as a "teaching artifact" per its own Design region, but it is never registered in DI — `program.cs:144-145` wires only `ActiveActionBehavior` and `EventStreamBehavior` as pipeline behaviors. It is dead code: unreferenced except by its own `global-suppressions.cs` entry (`CA1720` on its `Guid` property). Every generated app ships this unused, placeholder-named class.
- Suggestion: Delete it (the two real behaviors already demonstrate the pattern) or, if it earns its keep as a teaching artifact, rename it to something descriptive and register it so it actually runs like the repo's other demo features (which "ship unconditionally" per AGENTS.md and are live, not dormant).
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/grpc/projects/grpc-server/program.cs:46-56
- Description: `grpc-server` hand-rolls its own `AddCors`/`AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()` policy solely to add `.WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")`, duplicating `TimeWarp.Foundation.CorsPolicies.CorsPolicy.AnyPolicy` (source/foundation/foundation-server/cors-policy/cors-policies/any-policy.cs:27-41), which web-server and api-server both reuse via `CorsPolicy.Any.Name`.
- Suggestion: Extend `CorsPolicy.AnyPolicy` (or add a `CorsPolicy.AnyWithExposedHeaders`/overload accepting exposed headers) so grpc-server can consume the shared foundation policy instead of re-declaring the origin/method/header wildcarding inline.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/Directory.Build.props:8
- Description: A 30-entry `<NoWarn>` list (`CA1002;CA1024;CA1031;CA1032;CA1034;CA1040;CA1052;CA1062;CA1303;CA1304;CA1305;CA1311;CA1515;CA1715;CA1720;CA1725;CA1819;CA1823;CA1852;CA1861;CA2007;CA2016;CA2201;CA2211;CA2227;CA2234;CA2252;CA5394;RCS1102;RCS1194;RS0030`) carries no comment at all. Compare `tests/Directory.Build.props:17-37`, which annotates every single suppressed ID with a one-line reason — the exemplary pattern this file should follow. This is a template file: every generated app inherits this blanket relaxation of the root `AnalysisMode=All` policy with zero recorded rationale.
- Suggestion: Annotate each ID (or group) the way `tests/Directory.Build.props` does, or fold the genuinely-needed ones into per-project `<NoWarn>` with local justification and trim the rest.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/foundation/Directory.Build.props:4-7
- Description: `<!-- Suppress CA warnings that existed in original Common.Contracts/Common.Domain code -->` justifies 20 suppressed IDs (`CS1591;CA1002;CA1034;CA1036;CA1040;CA1062;CA1303;CA1304;CA1311;CA1715;CA1720;CA1725;CA1819;CA1852;CA1861;CA2007;CA2016;CA2201;CA2227;RS0030`) with a single "it was already like this" comment rather than a per-rule reason. Since `TimeWarp.Foundation.*` publishes as a NuGet package, this blanket relaxation ships to every consumer.
- Suggestion: Audit each ID against current foundation code; keep and justify the ones still needed (mirroring the tests/ file's per-ID comment style), drop the rest.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/global-suppressions.cs:11-13
- Description: Three assembly-level `SuppressMessage` attributes (`CA1052` on `Program`, `CA2000` on `Program.Main`, `CA1720` on `EventStreamBehavior<,>.Guid`) carry `Justification = "<Pending>"` — the Visual-Studio-generated placeholder was never filled in.
- Suggestion: Replace `"<Pending>"` with a real justification for each (or resolve the underlying warning and delete the suppression).
- Status: open

### Issue 7 — Severity: suggestion (grouped: suppressions inventory)
- File: repo-wide (source/, tests/, root/tests/foundation/container-apps `Directory.Build.props`, `.editorconfig`)
- Description: Full suppression inventory. Zero `[SuppressMessage]` outside the two `global-suppressions.cs` files found; no separate `GlobalSuppressions.cs` elsewhere; no `.editorconfig` `severity = none` lines lack a comment.
- Suggestion: See per-row verdicts; items marked "see Issue N" are broken out above/below as their own issue.
- Status: open

| Location | Diagnostic(s) | Justification present? | Verdict |
|---|---|---|---|
| source/libraries/timewarp-identity/credentials/credential.cs:66 | CA1819 | "Binary material is intentionally exposed as byte[] copies" | Valid, still true |
| source/libraries/timewarp-identity/ceremonies/webauthn/webauthn-registration-result.cs:39 | CA1819 | Same pattern as Credential | Valid |
| source/container-apps/web/projects/web-spa/global-usings.cs:5 | IDE0005 | Multi-file consumption across .cs/.razor | Valid |
| source/container-apps/web/projects/web-spa/features/event-stream/components/EventStream.razor:9 | CA1711 | Feature name, not a System.IO.Stream | Valid |
| source/foundation/foundation-domain/entities/base/entity.cs:101 | RCS1170 | EF reflection-writes Version via PropertyAccessMode.Property | Valid |
| source/container-apps/web/projects/web-spa/components/interfaces/i-static-route.cs:8 | CA1055 | Relative route string, not a URI | Valid |
| source/container-apps/web/projects/web-spa/features/identity/pages/login-page/LoginPage.razor:11 | CA1056 | Query-bound string, validated downstream | Valid |
| tests/common/timewarp-testing/global-suppressions.cs:6-7 | IDE0052 | "Construction the item will start it" (typo, terse) | Weak but plausible — field triggers side effects in its ctor; wording should be tightened |
| source/container-apps/web/projects/web-spa/global-suppressions.cs:10 | CA1720 | "Guid in this case is a good functional name" (on the dead `MyBehavior.Guid`) | Valid reasoning, but see Issue 2 (the type itself is dead) |
| source/container-apps/web/projects/web-spa/global-suppressions.cs:11-13 | CA1052, CA2000, CA1720 | `"<Pending>"` ×3 | **See Issue 6** |
| .editorconfig:298, 305-310, 314-316, 319, 337, 367, 391-394 | ca1308, RCS1138/39/40/41/42/1228, RCS1189/1181/1043, RCS1168, RCS1093, IDE0290, TW0001/TWA0004/15/16 | All carry multi-line documented rationale | Valid |
| source/analyzers/timewarp-architecture-analyzers/helpers/string-extensions.cs:24 | CA1308 | "ToCamelCase intentionally lowercases" | Redundant — `.editorconfig:298` already sets `dotnet_diagnostic.ca1308.severity = none` repo-wide. **See Issue 12.** |
| source/foundation/foundation-domain/entities/base/i-aggregate-root.cs:48-50 | CA1040 | Doc-comment above explains the marker-interface pattern | Redundant — `source/foundation/Directory.Build.props:7` already lists CA1040 in its project-wide `<NoWarn>`. **See Issue 12.** |
| Directory.Build.props:90 (root) | CA1014, CA1716, CA1724, CA1812, IL2026/2067-70/2075, IL3050-53, 1591, 1570-90, 1710-12, 1734, 0419 | Detailed rationale block | Valid |
| source/analyzers/Directory.Build.props:33 | NU5128 | "analyzer-only layout has no lib/" | Valid |
| source/foundation/Directory.Build.props:7 | CS1591 + 19 CA IDs | "existed in original code" (vague, blanket) | **See Issue 5** |
| source/container-apps/Directory.Build.props:8 | 30 CA/RCS/RS IDs | **No comment** | **See Issue 4** |
| source/container-apps/grpc/projects/grpc-server/grpc-server.csproj:5 | CA1050,1051,1848,1849,1591 | No comment | **See Issue 9** |
| source/container-apps/web/projects/web-spa/web-spa.csproj:39 | CA1848, CA1873, CA2254 | "high-throughput rules; browser SPA gains nothing; deliberate exception" | Valid |
| source/container-apps/web/projects/web-server/web-server.csproj:23 | 1591 | No comment | **See Issue 9** |
| source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj:11 | ASPIRE010 | Detailed Aspire-CLI-bundle rationale | Valid |
| tests/Directory.Build.props:17-37 | RS0030, CA2252, CA1707, CA2007, CA1303, CA1515, CA1052, CA1051, CA1062, CA1822, CA1852, CA2201, CA1859, CA1861, CA2234, CA2214, IDE0007, CA2000, NU1608, RCS1102, **TWA0004** | Every ID individually commented | Valid — exemplar; also the source of the TWA0004-off-in-tests policy referenced in Issue 10 |

### Issue 8 — Severity: suggestion (grouped: TODO/HACK/FIXME/XXX inventory)
- File: repo-wide (`grep -rnE "TODO|HACK|FIXME|XXX" source/ tests/ tools/dev-cli/`, excluding bin/obj)
- Description: 15 hits total (no HACK/FIXME/XXX hits — all are TODO). Full, exhaustive list below.
- Suggestion: See per-row verdicts.
- Status: open

| File:Line | Text | Verdict |
|---|---|---|
| source/container-apps/grpc/platform/codegen/protobuf-generation-hosted-service-server.cs:34 | `TODO automate the generation of these using Reflection` | Valid open item; already explained by the file's own Design region — could be folded in but not urgent |
| source/container-apps/web/projects/web-spa/components/pages/SideNavigationLink.razor:5 | `TODO use TimeWarp Source Gen and attributes once Chandu gets them finished` | Stale — names a specific person; the referenced source generator (`[Page]`/route generation) has since shipped. Delete or replace with a factual note |
| source/container-apps/web/projects/web-spa/components/pages/SideNavigationLink.razor:9 | `// TODO Add Bootstrap classes` | Stale — repo migrated to FluentUI v5 + plain CSS (Tailwind/Bootstrap both dropped); delete |
| source/container-apps/web/projects/web-spa/features/developer/components/user-claims-base.cs:5 | `TODO [2026-06]: Reassess UserClaimsBase after web-spa builds cleanly under AnalysisMode=All.` | Overdue — dated 2026-06, today is 2026-09-09, and the file's own comment says the decision (delete/integrate/leave) is "pending." Should become a kanban task rather than sit open past its own checkpoint date |
| source/container-apps/web/projects/web-spa/features/profile-menu/profile-menu-state/profile-menu-state.toggle.cs:9 | `// inline TODO (transitions and NotifyLossOfInterest).` (Design region cross-reference) | Valid — tracks real missing UX behavior, already surfaced in the Design region |
| source/container-apps/web/projects/web-spa/features/profile-menu/profile-menu-state/profile-menu-state.toggle.cs:29 | `// TODO: Transitions and NotifyLossOfInterest not working` | Valid — same item as above; candidate to become a kanban task instead of a standing TODO |
| source/container-apps/web/features/todo-items/todo-item-dto-contracts.cs:7 | `// DTO — see the TODO above the class.` | Cross-reference to the row below; not independently actionable |
| source/container-apps/web/features/todo-items/todo-item-dto-contracts.cs:15 | `TODO: Revist the Mixins now that we have established better patterns` | Stale — see Issue 11 |
| source/container-apps/web/features/analytics/track-event/track-event-handler-application.cs:25 | `TODO implement code here that formats and sends data to your favorite Analytics tool` | Valid — deliberate template extension point (Design region confirms "deliberate no-op"); consistent with the same instructional-TODO style used in `example-policy.cs` below, not a design question, so no Open Questions region needed |
| source/foundation/foundation-application/abstractions/i-current-user-service.cs:9 | `// TODO: Should this be a strongly typed UserId?` | Should move to `#region Open Questions` — this is an unresolved design question (Guid vs. typed id), not a work item, per the Agent Context Regions convention |
| source/foundation/foundation-server/cors-policy/cors-policies/example-policy.cs:30 | `// #TODO add all of your domains we are using localhost here` | Valid — instructional placeholder for template consumers filling in `ExamplePolicy`, same pattern as the analytics one above |
| tests/common/timewarp-testing/scoped-sender.cs:16 | `// TODO: Implement this method when needed` | Valid — `ISender.CreateStream<T>` stub required only for interface compliance; genuinely unneeded until streaming is exercised |
| tests/common/timewarp-testing/scoped-sender.cs:23 | `// TODO: Implement this method when needed` | Valid — paired non-generic overload, same reasoning |
| tools/dev-cli/endpoints/verify-samples-command.cs:25 | `// TODO: Implement sample verification logic specific to this repo` | Valid open item, but see Issue 1 — the surrounding output actively misrepresents the stub as having succeeded |

### Issue 9 — Severity: nit
- File: source/container-apps/grpc/projects/grpc-server/grpc-server.csproj:5, source/container-apps/web/projects/web-server/web-server.csproj:23
- Description: `<NoWarn>$(NoWarn);CA1050;CA1051;CA1848;CA1849;1591</NoWarn>` (grpc-server) and `<NoWarn>$(NoWarn);1591</NoWarn>` (web-server) carry no comment, unlike the well-justified `web-spa.csproj:39` and `aspire-app-host.csproj:11` suppressions in the same tree.
- Suggestion: Add a one-line reason for each (missing XML doc comments on generated/hosted types is the likely reason for `1591`, and generated code is the likely `CA1050/1051/1848/1849` reason for grpc-server — confirm and record it).
- Status: open

### Issue 10 — Severity: nit
- File: tests/common/timewarp-testing/scoped-sender.cs:1, tests/common/timewarp-testing/global-suppressions.cs:1
- Description: Neither file carries a `#region Purpose` block. TWA0004 is deliberately suppressed repo-wide for `tests/` (`tests/Directory.Build.props:32`, well-commented: "#region Purpose not yet adopted for test files — separate decision"), so this does not fail the build — but most sibling files in the same `tests/common/timewarp-testing` project (e.g. `host-graph-factory.cs`) do carry Purpose regions, so these two are inconsistent with the de facto in-project convention rather than the enforced one.
- Suggestion: Add a one-line Purpose to both for consistency with the rest of the project, next time either file is touched.
- Status: open

### Issue 11 — Severity: nit
- File: source/container-apps/web/features/todo-items/todo-item-dto-contracts.cs:15
- Description: `// TODO: Revist the Mixins now that we have established better patterns` (also typo'd "Revist") is redundant — the file's own `#region Design` block (lines 5-10) already documents that the endpoint-centric pattern supersedes this DTO and that the mixin idea "was not adopted." The bare TODO asks a question the Design region already answers.
- Suggestion: Delete the TODO line; the Design region is the authoritative record.
- Status: open

### Issue 12 — Severity: nit
- File: source/analyzers/timewarp-architecture-analyzers/helpers/string-extensions.cs:24; source/foundation/foundation-domain/entities/base/i-aggregate-root.cs:48-50
- Description: Two local `#pragma warning disable` pairs are dead weight — each suppresses a diagnostic already silenced at a broader scope. `string-extensions.cs:24` disables `CA1308` around `ToCamelCase`, but `.editorconfig:298` already sets `dotnet_diagnostic.ca1308.severity = none` for the whole repo. `i-aggregate-root.cs:48-50` disables `CA1040` around the `IAggregateRoot` marker interface, but `source/foundation/Directory.Build.props:7` already lists `CA1040` in that project tree's `<NoWarn>`. Neither pragma is wrong, just redundant — harmless today, but a future analyzer-severity change at the broader scope could silently stop being reflected at the pragma site (or vice versa), and it's one more thing for an app author copying this pattern to puzzle over.
- Suggestion: Drop both local pragmas; the broader suppression already covers them. Keep the explanatory prose (doc-comment / inline comment) if it has standalone value.
- Status: open

## Checked clean
- Zero `using MediatR` anywhere in source/ or tests/ (TimeWarp.Mediator only).
- Zero `.Should()` / `using FluentAssertions` anywhere (Shouldly only).
- Zero direct `.Send(...)` calls in `web-spa` client code; zero `#pragma warning disable TWA0022` escapes.
- Zero hand-written `BaseEndpoint` classes; all endpoints generate from `[ApiEndpoint]` contracts.
- Zero re-validation in handlers (`IValidator<>` not referenced from any `*-handler-application.cs`); validation stays on `FluentValidationBehavior`.
- Zero inline `new JsonSerializerOptions{...}` at the TimeWarp contract wire seam; all Web/Api contract (de)serialization goes through `ContractSerializationDefaults`. The three local `JsonSerializerOptions` instances found (`source/libraries/timewarp-402/**`, `weather-forecasts-state.debug.cs`) are outside that seam — x402 external-protocol wire format and a Redux-DevTools debug snapshot format, respectively — and are each justified in their own Design regions.
- Zero leftover Tailwind utility-class strings in `.razor`/`.cs`/`.cshtml` (spot-checked hits for `flex`/`px-`/`bg-`/etc. were all false positives or non-existent).
- All 36 `[ApiEndpoint]` contracts carry exactly one auth marker; every `[EndpointAllowAnonymous(reason)]` reason read is substantive (ceremony bootstrapping, tip-jar no-security-surface, analytics pre-auth capture, demo zero-setup) — none are hollow ("demo"/"test"/empty).
- No stale Agent Context Region found: all files under `web-spa/features/identity/**` and `web/features/authorization/**` (the SPA-identity-fold surfaces, task 132-001), `host-graph-factory.cs`, and all three `program.cs` bootstraps have Purpose/Design regions matching their code; same for a 25-file spread sample across `features/`, `platform/`, `foundation/`, and `libraries/`.
- `source/libraries/timewarp-402` and `timewarp-identity` are excluded from template output (`.template.config/template.json`); every `ProjectReference` from `web-server`, `web-spa`, `api-server`, `grpc-server`, and the intermediate contracts/application/infrastructure projects that do reference them uses the documented `UseIdentityPackages`/`UseX402Packages` dual-mode conditional — no template-included file breaks package-mode generation.
- Naming spot-check (~15 files across `features/`/`foundation/`): no underscore-prefixed private fields, no PascalCase locals, no confusing type-stem violations beyond the already-covered `MyBehavior` (Issue 2).
- `tools/dev-cli`: no hard-coded `/home/`-style absolute paths; the one `.ps1` grep hit (`run-command.cs:2,9`) is a comment noting it replaces the legacy `Run.ps1`, not a live shell-out.
- Roslynk `find_dead_code` (whole-solution, default confidence filtering) surfaced no confirmed dead code in `source/` beyond `MyBehavior` (found manually, Issue 2) — its one other product-code candidate, `aspire-app-host`'s `ResourceBuilderExtensions.WithScalar`, is a false positive (Roslynk doesn't resolve the extension-method call site at `program.cs:81`); all other candidates were test-registration methods (Jaribu framework-invoked, expected to show as "no references").
- No commented-out dead code blocks found in `source/`/`tests/` beyond the already-explained `#if false` teaching sketch in `user-claims-base.cs` and the standard, upstream-mirrored commented-out optional-exporter blocks in `aspire-service-defaults/extensions.cs` (both self-documented in their own Design regions as intentional).
- `program.cs` bootstraps for web-server/api-server/grpc-server are not duplicative in a way that needs fixing — they already delegate shared setup to `CommonServerModule`/foundation-server and differ where the hosts genuinely differ (web-server is much larger because it also hosts the Blazor SPA and prerendering).
