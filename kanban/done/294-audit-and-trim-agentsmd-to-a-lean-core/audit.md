# AGENTS.md audit (task 294)

Measured on this branch before the rewrite: 33,098 bytes, 408 lines, 4,000 words.
After: 3,566 bytes, 54 lines, 481 words (under the 8 KB target).

The file was a second copy of skills, analyzer release notes, MSBuild comments, and `dev`
command help. A few of those copies had drifted. The lean core keeps what every run needs:
what the repo is, hard rules nothing else enforces, and pointers.

## Verdicts

| Section | Verdict | Reason |
|---------|---------|--------|
| Preamble (`CLAUDE.md` includes this file) | keep | Still true. `CLAUDE.md` is `@AGENTS.md`. |
| Agent communication — no calendar estimates | keep, trimmed | `tw-analysis-report` bans estimates in reports only. `tw-kanban` bans an Estimate field. Nothing else stops an agent from quoting days in chat. |
| What this repo is | keep, trimmed | Identity of the template, flag regions, and "demos are not flags." Removal steps already live in `tw-slice-isolation`. |
| Build / run / test | trim to a pointer | Command behavior is `dev --capabilities` and the command source (`test-command.cs`, `db-nuke.cs`). Port numbers and C-create already live in `co-located-jaribu-runfiles.md`. |
| Before opening a PR | trim to a pointer | Procedure is `tw-pr`. The three repo gates stay as one row because `tw-pr` tells the agent to read this file for the repo's gates. |
| Stack | trim | One paragraph of stack facts. The long test, auth, and endpoint prose moved or was already in a skill. SDK version string removed so it cannot drift from `global.json` (it matched on this date: `11.0.100-rc.1.26425.128`). |
| Layout (tree, grammar, unrouted `tests` layer, features substrate) | delete from this file | Already the body of `tw-feature-placement`, including the deletion litmus and the `unroutedLayers` enforcement gap. |
| File-naming exception list | delete from this file | `tw-csharp` owns the exception table. `TW0001` severity in this repo is `.editorconfig` (`warning`, and warnings are errors). `kebab-path-names` is `ganda repo audit`. |
| Platform packages | delete from this file | SSOT is root `Directory.Build.props` (dual-mode switches), `Directory.Packages.props` (pins equal `<Version>`), `msbuild/timewarp-platform-packages.props` (sourceName-safe ids), and the Generators csproj description ("attach only where generators should run — not repo-wide"). A new skill would have been a third copy. |
| Key patterns (contracts, IAssemblyMarker, serializer options) | move the two sentences that were only here; delete the rest | Contract shape is `tw-web-api-contracts`. `IAssemblyMarker` is the comment in root `Directory.Build.targets`. "Never declare seam options inline" was only here; it is now on the serialization step in `tw-web-api-contracts`. |
| Enforcement catalog (TWA / TWE / SG tables) | delete from this file | The catalog is `AnalyzerReleases.Unshipped.md` in each analyzer project, plus the diagnostic text the build prints. Copying it here is why the file grew and why a third copy would drift. Retired ids that lived only in this file are now `;` comments on those Unshipped files. Per-rule workflow stays in the skill that owns the rule. |
| Slice isolation and aggregate blurbs | delete from this file | `tw-slice-isolation` and `tw-aggregate-pattern`. |
| Agent context regions | trim to a pointer, plus the id correction | The maintenance rule is `tw-agent-context-regions`. This file now says the id is **TWA0004**, because the flow skill says TWPA0004. |
| Definition of Done | delete from this file | Endpoint done is the `tw-web-api-contracts` checklist. Client done is `tw-blazor`. |
| Task management | trim to a pointer | The procedure in this section contradicted `tw-kanban` (see below). |
| Documentation | trim | Keep the ship/don't-ship facts: `skills/` pack, `skills/*/analysis/` and `documentation/` do not, this file does not. Dropped the "nine skills" count so it cannot rot. |

## Contradictions

Each one quotes both sides. The winner is the side that matches the code or the skill that owns the procedure.

### 1. How to open a kanban task

`AGENTS.md` Task management (before):

> always `ganda kanban create "title"` (it assigns the number — never hand-number), then `move`/`done` to transition.

`kanban/overview.md` (before):

> New work: `ganda kanban create "title"` (it assigns the number).

`tw-kanban`:

> Create is deprecated alias of reserve + first-claim. Prefer reserve/claim.

**Winner: `tw-kanban`.** Reserve allocates the id. `create` still works and is the deprecated alias. This file and `kanban/overview.md` now point at the skill.

### 2. Purpose-region diagnostic id

`purpose-region-analyzer.cs`: `public const string DiagnosticId = "TWA0004"`.

Flow skill `tw-agent-context-regions`:

> The timewarp-architecture template enforces Purpose presence at build time (**TWPA0004**).

**Winner: TWA0004.** The flow skill is not in this repo, so it was not edited. The lean core states the real id so an agent that loads both does not treat them as equal.

### 3. Purpose on every file, or only non-trivial files

`purpose-region-analyzer.cs` Design region:

> The rule is deliberately UNIVERSAL rather than "non-trivial files only".

Flow skill `tw-csharp`:

> Non-trivial source files carry `#region Purpose`.

Flow skill `tw-agent-context-regions` agrees with the analyzer ("no triviality exemption"). `tw-csharp` does not.

**Winner: the analyzer (every source file; generated code is exempt).** `tw-csharp` is a flow skill and was not edited here.

### 4. Test file names

Flow skill `tw-jaribu`:

> Format: `{sut}.{action}.cs` (kebab-case) … `shell-builder.capture-async.cs`

This repo, enforced by TWA0015/TWA0016 and the membership guard for co-located runfiles, and by `tw-csharp` kebab for suite files: `create-role-tests.cs`, `get-profile-session-tests.cs`.

**Winner: this repo's names.** Dotted `{sut}.{action}.cs` is the jaribu repo's layout. Do not rename architecture tests to match it.

### 5. `Lazy` / process-static hosts

Flow skill `tw-jaribu`:

> Static/`Lazy` remains fine when no dispose is needed.

`skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`:

> Never share ASP.NET hosts across classes via process-static / `Lazy` / assembly singletons.

The same file allows a Testcontainers postgres `Lazy` because Ryuk reaps the container, and says not to cite that as precedent for Kestrel hosts.

**Winner: the local reference for anything that must be disposed (hosts).** `tw-jaribu` still wins when nothing needs dispose. The old AGENTS paragraph said the same thing as the reference; it was a duplicate, not a third rule.

### 6. `endpoint` filename function "currently unused"

`tw-feature-placement` (before):

> the reserved `endpoint` function above is kept documented but currently unused.

The tree has `challenge-entra-endpoint-server.cs`, `sign-out-browser-session-endpoint-server.cs`, and `sign-out-antiforgery-token-endpoint-server.cs`, each an `EndpointWithoutRequest`. The old Stack section named those three as the allowed hand-written exception.

**Winner: the code.** The skill now says the function is for those browser-protocol shims. The rule itself moved to `tw-web-api-contracts` ("Browser-protocol endpoints").

### 7. TWA0004 does not catch a misplaced file

`tw-feature-placement` agent workflow (before):

> the membership guard and TWA0004 catch misplacement at build time

TWA0004 is "source file lacks a `#region Purpose` block." Misplacement is the membership guard plus TWA0015/TWA0016.

**Winner: the analyzers.** The skill sentence now names TWA0015/TWA0016.

### 8. A second HTTP verb on the server

`tw-web-api-contracts` (before):

> The verb must match the server endpoint.

`endpoint-coverage-analyzer.cs`:

> TWA0005 (MVC verb mismatch) was retired with BaseEndpoint … FastEndpoints are source-generated from the contract's [ApiRoute] verb, so hand-written verb drift cannot occur. ID TWA0005 is reserved and must not be reused.

**Winner: the analyzer comment.** The skill now says the verb lives on `[ApiRoute]` and TWA0005 is reserved. The reserved-id line is also on `AnalyzerReleases.Unshipped.md`.

### 9. Board Definition of Done vs the contracts skill

`kanban/overview.md` still lists a required Mapper, a hand-written Endpoint, and RequestValidator unit tests for every rule.

`tw-web-api-contracts`:

> Do **not** add isolated validator unit tests in the contracts test project.

The same skill says both hosts generate FastEndpoints and that handlers do not re-validate.

**Winner: `tw-web-api-contracts` (and `tw-blazor` for a client feature).** The overview now says so above the old checklist. The checklist was left in place as the older board template rather than deleted out from under people who still read it.

## Checked, and not contradictions

- **Auth fail-closed.** The old file said a missing marker "emits no auth config at all rather than defaulting to anonymous." The contracts skill says the generator emits nothing and FastEndpoints' own default (auth required) applies. Same fact. The skill is the precise one. The vaguer sentence was not copied forward.
- **`IAuthApiRequest` does not secure the server.** The old file and `tw-web-api-contracts` agree. TWA0014 enforces the forbidden pairing.
- **TW0001.** The old file said `.editorconfig` sets it to warning. That line is still `dotnet_diagnostic.TW0001.severity = warning`.
- **Estimate fields.** The old file and `tw-kanban` agree: no Estimate field.
- **SDK pin.** The old file's `11.0.100-rc.1.26425.128` matched `global.json` on 2026-10-10. The lean file points at `global.json` instead of repeating the pin.

## What moved, and what was not copied

| From the old file | New home |
|-------------------|----------|
| Browser-protocol `EndpointWithoutRequest` exception | `skills/tw-web-api-contracts/SKILL.md` |
| Never declare contract-seam `JsonSerializerOptions` inline | same skill, serialization step |
| In-proc vs closed-box host lanes, mock principal header | `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md` (C-create / C-share were already there) |
| Retired TWA0005, TWA0029–0031 | comment on `source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md` |
| Retired TWE001, TWE004, TWE012–014 | comment on `source/analyzers/timewarp-architecture-analyzers/AnalyzerReleases.Unshipped.md` (the longer note was already in `diagnostic-descriptors.cs`) |

The diagnostic tables were not pasted into a skill. `AnalyzerReleases.Unshipped.md` is already that catalog. Pointers in `tw-feature-placement` and `tw-slice-isolation` that said "see the AGENTS.md table" now point at the Unshipped file.

Design-region comments that said "AGENTS.md fixture-lifetime default" now cite the co-located runfile reference. `purpose-region-analyzer.cs` cites `tw-agent-context-regions` instead of this file for the unenforced Design half.

## Template pack and `ganda agents` sync

Deliberately different, not a missed copy.

- The template pack allow-list in `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj` includes `source/`, `tests/`, `skills/`, `msbuild/`, and a fixed set of root files. It does not include `AGENTS.md` or `CLAUDE.md`. There is no second `AGENTS.md` under the template. Generated apps get the skills, which is where the situational rules now live. This file is the monorepo guide (kanban, `ganda repo audit`, `dev check-version`) and stays unpackaged. `readme.md` says that.
- `ganda agents targets` lists three files, all under timewarp-flow: opencode `AGENTS.md`, claude `CLAUDE.md`, grok `AGENTS.md`. Those are global preference copies. This repo is not a sync target. Per-repo `AGENTS.md` stays authored here.

## Not changed

`readme.md` badge still says `dotnet-10.0`. `global.json` and the projects target `net11.0`. That badge is outside this audit's edit. Flow skills `tw-agent-context-regions` (TWPA0004), `tw-csharp` ("non-trivial" Purpose), and `tw-jaribu` (dotted test names, .NET 10 in the description) are outside this repo.
