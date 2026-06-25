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

- [ ] Convert the 3 template scaffolding files in `Feature.Endpoint/Server.Tests/`
      to Shouldly (swap `using`, rewrite `.Should()` assertions)
- [ ] Check whether `TimeWarp.Architecture.Template.Tests` code actually uses
      FluentAssertions; convert any real usages to Shouldly
- [ ] Replace the FluentAssertions `PackageReference` in the template-tests
      csproj with Shouldly (use a current Shouldly version)
- [ ] Remove the `FluentAssertions` `PackageVersion` from root `Directory.Packages.props`
- [ ] Verify no `FluentAssertions` / `using FluentAssertions` / unconverted
      `.Should()` references remain repo-wide (excluding `artifacts/`, `obj/`)
- [ ] Confirm `dev build` is still green (note: the active solution doesn't build
      the templates tree, so also build/restore the templates project directly if feasible)

## Notes

- Closes out the FluentAssertions item from task **041 — methodically update
  nuget packages**. The original outdated report flagged
  `FluentAssertions 6.7.0 → 8.10.0`; we are replacing rather than upgrading
  because of the v8 license change.
- The active solution is unaffected at runtime — this is legacy-template and
  CPM-hygiene cleanup that prevents new scaffolded features from depending on
  FluentAssertions.

## Session

- Created: 2026-06-25 (task spun off from 041)
