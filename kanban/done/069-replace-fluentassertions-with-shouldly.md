# Replace FluentAssertions with Shouldly

## Description

Remove FluentAssertions from the repository and replace every usage with
Shouldly. This is driven by the FluentAssertions v8 license change (commercial
licensing required for many uses) — rather than upgrade past v6.7.0, we
standardize on Shouldly, which the active solution already uses exclusively.

**Current state (2026-06-25):**
- The active solution (`source/` + `tests/`, built by `dev build` /
  `timewarp-architecture.slnx`) **already uses Shouldly only** — no
  FluentAssertions there.
- `Shouldly` 4.3.0 is already declared in the root CPM manifest.
- FluentAssertions lingers **only in the legacy `TimeWarp.Templates/` tree**,
  which is not part of the active solution and is not built by `dev build`.

## Scope / Footprint

FluentAssertions `6.7.0` appears in exactly these places:

1. **Root CPM declaration (orphaned for active solution):**
   - `Directory.Packages.props` — `<PackageVersion Include="FluentAssertions" Version="6.7.0" />`

2. **Legacy template-tests project (inline-versioned, not in any buildable solution):**
   - `TimeWarp.Templates/Tests/TimeWarp.Architecture.Template.Tests/TimeWarp.Architecture.Template.Tests.csproj`
     — `<PackageReference Include="FluentAssertions" Version="6.7.0" />`
     (also on legacy deps: Fixie 3.3.0, Fixie.TestAdapter 3.3.0, TimeWarp.Fixie 1.0.2)
   - Verify whether this project's own `.cs` (e.g. `TemplateTest.cs`) actually
     calls FluentAssertions; the reference may be transitive/unused.

3. **Template scaffolding files (these GENERATE feature tests for `dotnet new`):**
   - `TimeWarp.Templates/Source/TimeWarp.Architecture.Template/templates/Feature.Endpoint/Server.Tests/__RequestName__Endpoint_Tests.cs`
     — `using FluentAssertions;` + 3 `.Should()` sites
   - `TimeWarp.Templates/Source/TimeWarp.Architecture.Template/templates/Feature.Endpoint/Server.Tests/__RequestName__Handler_Tests.cs`
     — `using FluentAssertions;`
   - `TimeWarp.Templates/Source/TimeWarp.Architecture.Template/templates/Feature.Endpoint/Server.Tests/__RequestName__RequestValidator_Tests.cs`
     — `using FluentAssertions;` + 1 `.Should()` site

   > These templates emit test files into newly-scaffolded features, so leaving
   > them on FluentAssertions would reintroduce the dependency for every new
   > feature created from the template.

## Assertion Conversion Reference (FluentAssertions → Shouldly)

| FluentAssertions                       | Shouldly                              |
|----------------------------------------|---------------------------------------|
| `using FluentAssertions;`              | `using Shouldly;`                     |
| `x.Should().Be(y)`                     | `x.ShouldBe(y)`                       |
| `x.Should().BeTrue()`                  | `x.ShouldBeTrue()`                    |
| `x.Should().BeFalse()`                 | `x.ShouldBeFalse()`                   |
| `s.Should().Contain("errors")`         | `s.ShouldContain("errors")`           |
| `x.Should().NotBeNull()`               | `x.ShouldNotBeNull()`                 |

(Known sites in scope: `.Should().Be(...)`, `.Should().Contain(...)`,
`.Should().BeTrue()`. Convert any others encountered the same way.)

## Checklist

- [x] Convert the 3 template scaffolding files in `Feature.Endpoint/Server.Tests/`
      to Shouldly (swap `using`, rewrite `.Should()` assertions)
- [x] Convert the `Feature.AutoCrud/GeneratedCode/.../Endpoint.Test/` templates
      (9 files — see Results; this was NOT in the original scope)
- [x] Check whether `TimeWarp.Architecture.Template.Tests` code actually uses
      FluentAssertions — it did not; removed the unused `PackageReference`
- [x] Remove the `FluentAssertions` `PackageVersion` from root `Directory.Packages.props`
- [x] Update remaining doc/metadata references (Overview.md, .ai references.md, nugets.md)
- [x] Verify no `FluentAssertions` / `using FluentAssertions` / unconverted
      `.Should()` references remain repo-wide (excluding `artifacts/`, `obj/`)
- [x] Confirm `dev build` is still green

## Notes

- Closes out the FluentAssertions item from task **041 — methodically update
  nuget packages**. The original outdated report flagged
  `FluentAssertions 6.7.0 → 8.10.0`; we are replacing rather than upgrading
  because of the v8 license change.
- The active solution is unaffected at runtime — this is legacy-template and
  CPM-hygiene cleanup that prevents new scaffolded features from depending on
  FluentAssertions.

## Results

- **Initial scope was incomplete.** The first investigation found only the 3
  `Feature.Endpoint/Server.Tests/` template files. A deeper grep surfaced a
  second, larger set under
  `Feature.AutoCrud/GeneratedCode/Test/ServerTests/Endpoint.Test/` — **9 files**
  (3 Endpoint, 3 RequestValidator, 3 Handler). All converted.
- **Conversion pattern applied** (FluentAssertions → Shouldly):
  `.Should().Be(x)` → `.ShouldBe(x)`, `.Should().Contain(x)` → `.ShouldContain(x)`,
  `.Should().BeTrue()` → `.ShouldBeTrue()`. Endpoint/Validator files swapped
  `using FluentAssertions;` → `using Shouldly;`; Handler files had an unused
  `using FluentAssertions;` which was removed outright.
- **Package cleanup:** removed unused FluentAssertions `PackageReference` from
  `TimeWarp.Architecture.Template.Tests.csproj`; removed `PackageVersion` from
  root `Directory.Packages.props`. No Shouldly reference needed to be added to
  the templates project (its own code asserts nothing).
- **Docs/metadata updated:** `TimeWarp.Templates/Documentation/.../Overview.md`,
  `TimeWarp.Architecture/.ai/other/references.md`, `.ai/other/nugets.md`.
- **Verification:** zero `FluentAssertions` / unconverted `.Should()` remain
  repo-wide (excl. `artifacts/`, `obj/`, kanban history). `dev build` green.
- Note: the templates tree is not part of `timewarp-architecture.slnx`, so it is
  not compiled by `dev build`; verification was via exhaustive grep + the active
  build staying green after removing the root CPM declaration.

## Session

- Created: 2026-06-25 (task spun off from 041)
- Implementation: 2026-06-25
