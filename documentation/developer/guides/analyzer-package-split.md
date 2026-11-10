# Analyzer package split

Maintainer record of which `TWA*` diagnostics stay in `TimeWarp.Architecture.Analyzers` and which generic style rules move to `TimeWarp.SourceGenerators`. Generated apps do not ship `documentation/`. The consumer-facing note is the package description and the header of `AnalyzerReleases.Unshipped.md` in `source/analyzers/timewarp-architecture-convention-analyzers/`.

## Decision

Split by what the rule checks. A rule that checks C# file or name shape, and does not mention an Architecture concept, is generic. A rule that checks contracts, endpoints, aggregates, slices, Aspire, the template, FluentUI page structure, or a TimeWarp domain base type stays in `TimeWarp.Architecture.Analyzers`.

Diagnostic IDs stay `TWA*`. Do not rename them to `TW*`. `.editorconfig` and globalconfig keys keep working without an alias:

- `dotnet_diagnostic.TWA0004.severity`
- `dotnet_diagnostic.TWA0023.severity`
- `dotnet_diagnostic.TWA0038.severity`
- `dotnet_diagnostic.TWA0038.max_lines`

`timewarp-source-generators` task **034** ports the generic set. `TimeWarp.SourceGenerators` 1.0.0-beta.13 reports TWA0004, TWA0023, and TWA0038. This repo pins that version in `Directory.Packages.props`. The three analyzers are not compiled into `TimeWarp.Architecture.Analyzers`. Architecture references both packages (`Directory.Build.props`). Two analyzers with the same ID in one compilation report twice, so the deletion and the pin bump are one change. `timewarp-state` task **153** drops `TimeWarp.Architecture.Analyzers` and takes TWA0004 from that SourceGenerators release.

## Move set (generic)

Move set: TWA0004, TWA0023, and TWA0038.

| ID | Analyzer | Default | Removed from this package |
|----|----------|---------|---------------------------|
| TWA0004 | `PurposeRegionAnalyzer` | Warning, enabled | `purpose-region-analyzer.cs`, `purpose-region-analyzer-tests.cs` |
| TWA0023 | `TypeStemIdentifierAnalyzer` | Warning, disabled | `type-stem-identifier-analyzer.cs`, `type-stem-identifier-naming-analyzer-tests.cs`, `type-stem-identifier-exemptions-analyzer-tests.cs`, `type-stem-identifier-test-fixture.cs` |
| TWA0038 | `FileLengthAnalyzer` | Warning, enabled | `file-length-analyzer.cs`, `file-length-analyzer-tests.cs` |

TWA0004 requires a `#region Purpose` block in every non-generated C# file. This package does not report it. `timewarp-state` pins published `TimeWarp.Architecture.Analyzers` 2.0.0-beta.31, which still contains the rule, and sets `dotnet_diagnostic.TWA0004.severity = error`. Task 153 switches that pin to `TimeWarp.SourceGenerators`.

TWA0023 requires an identifier to end with its type stem (interfaces drop a leading `I`). It is the `tw-csharp` naming rule. Opt-out matches the simple name `TypeStemIdentifierAttribute`. The attribute type stays in `TimeWarp.Architecture.Attributes` (`type-stem-identifier-attribute.cs`). Do not move that package. SourceGenerators tests stub the attribute. State sets the severity to none.

TWA0038 flags a file over 500 lines. `dotnet_diagnostic.TWA0038.max_lines` overrides the limit. Path segments `/obj/` and `/goldens/` are exempt. Generated code is skipped. State does not set this severity. The rule is enabled in `TimeWarp.SourceGenerators`, so a repo that treats warnings as errors and leaves the severity unset gets the warning.

## Stays in TimeWarp.Architecture.Analyzers

Reported by `timewarp-architecture-convention-analyzers`:

| ID | Analyzer | Why it stays |
|----|----------|----------------|
| TWA0002, TWA0003 | contract-nullability-validator | Contract nullability versus validator rules |
| TWA0006 | endpoint-coverage | Routed contract has no server endpoint |
| TWA0007 | aspire-resource-name | Aspire resource name is a `ServiceNames` constant |
| TWA0008 | template-conditional-token | `dotnet new` conditional tokens in comments or strings |
| TWA0009 | slice-isolation | A slice references another product slice |
| TWA0010 | template-flag-constants | A template flag is missing from `DefineConstants` |
| TWA0011, TWA0012 | aggregate-invariants | Aggregate `Invariants` validator shape |
| TWA0013, TWA0014, TWA0020 | endpoint-auth-posture | `[ApiEndpoint]` auth markers and `[ClientOnlyContract]` |
| TWA0015, TWA0016 | feature-filename-grammar | Feature filename function segments |
| TWA0021 | mock-authentication-registration | Mock auth registration site |
| TWA0022 | spa-mediator-send | SPA mediator `Send` versus a generated action set |
| TWA0024 | endpoint-authorize-policy-agreement | `[EndpointAuthorize]` policy is registered |
| TWA0025 | page-local-message-bar | Outcome message bars belong in the shell host |
| TWA0026, TWA0027 | component-side-effect | Components dispatch actions; opt-out needs a reason |
| TWA0028 | enumeration-member-shape | Members of `TimeWarp.Foundation.Enumerations.Enumeration` are `public static readonly` fields. Name match, no assembly reference. A repo without that base type never fires it. Not generic C# style. |
| TWA0032 | direct-fluent-text-area | `FluentTextArea` renders through the shared `TextArea` |
| TWA0033, TWA0034 | referenceable-resource | `[ReferenceableResource]` shape and unique `@` prefix |
| TWA0035 | operation-attribute | Hosted contract request carries `[Operation]` |
| TWA0036 | iso-date-rendering | Dates and times render as ISO 8601, including Razor `RenderTreeBuilder.AddContent`. Template rendering policy. State sets the severity to none. |
| TWA0037 | direct-fluent-message-bar | `FluentMessageBar` renders through the shared `MessageBar` |
| TWA0039–TWA0043 | contract-agreement | Aggregate root, command, and query projection agreement |

## Do not move with the style split

Retired. The ID is reserved. `TWA0005` remains a constant on `EndpointCoverageAnalyzer` and is not in `SupportedDiagnostics`.

- TWA0005
- TWA0029, TWA0030, TWA0031

`timewarp-architecture-analyzers` (package `TimeWarp.Architecture.Generators`) reports these. They are generator diagnostics, not the style split:

- TWA0001 partial class declaration
- TWA0017, TWA0018, TWA0019 ingress route prefix

No `TWA*` id above TWA0043 is declared in either analyzer project.

## Consumers

On this machine the only `PackageReference` to `TimeWarp.Architecture.Analyzers` outside this repo is `timewarp-state` (`Directory.Packages.props` pin `2.0.0-beta.31`). That published package still contains TWA0004. State turns the other `TWA*` severities it lists to none. It has no `TWA0038` or `TWA0039`–`TWA0043` entries.

A style-only consumer takes `TimeWarp.SourceGenerators` 1.0.0-beta.13 or later, which reports TWA0004, TWA0023, and TWA0038, and does not reference `TimeWarp.Architecture.Analyzers`. State does not need a newer build of this package for those rules. Task 153 drops the reference. The unpublished repo version `2.0.0-beta.32` is the build that omits the three rules.
