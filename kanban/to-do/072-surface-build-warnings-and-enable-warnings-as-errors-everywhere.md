# 072: Surface build warnings + drive to warnings-as-errors everywhere

## Why

CI/CD is green, but the build emits **hundreds of warnings** that are easy to ignore. Two coupled
problems: we can't see them locally, and they aren't enforced where they leak.

## 1. `dev build` should show output like `dotnet build`

`tools/dev-cli/endpoints/build-command.cs` runs `DotNet.Build()...CaptureAsync(Ct)` and only writes
`result.Combined` when `--verbose` is passed (or on failure). So a normal `dev build` swallows all the
warnings — they're invisible unless you remember `-v`. It should **stream the build output by default**
(pass-through), the way `dotnet build` does. Amuru exposes `PassthroughAsync` / `TtyPassthroughAsync`
for this. Apply the same to `dev test` / `dev workflow` where they capture-and-hide.

## 2. Warnings-as-errors everywhere (after fixing the backlog)

`TreatWarningsAsErrors` + `CodeAnalysisTreatWarningsAsErrors` are already `true` in the root
`Directory.Build.props`, so **source/ is clean**. The warnings come from projects/areas that opt out or
that the root list lets through:
- **Test projects** appear to relax it — e.g. `CA1303` (literal strings to `Console.WriteLine`),
  `CA1861` (constant array args), and `NU1608` (transitive version conflicts: Oakton/FakeItEasy/Hosting).
- Some are NuGet (`NU*`) warnings, which `TreatWarningsAsErrors` does NOT cover.

Goal: make warnings-as-errors apply to **all** projects (tests included), but only after the backlog is
cleared — otherwise the build goes red on ~100s of pre-existing warnings.

## Checklist

- [ ] `dev build` (+ test/workflow): stream output by default instead of capture-and-hide; keep a quiet
      mode if desired, but warnings must be visible on a normal run.
- [ ] Inventory the warnings (run a full build with output, group by rule: CAxxxx, IDExxxx, NUxxxx).
- [ ] Fix them rule-by-rule (or justify a scoped `NoWarn` with a comment) — tests are the bulk.
- [ ] Resolve the `NU1608` transitive version conflicts (Oakton vs Microsoft.Extensions.Hosting 10,
      AutoFixture.AutoFakeItEasy vs FakeItEasy 9) — pin or upgrade.
- [ ] Once clean, enable `TreatWarningsAsErrors` (and `NU` promotion via `WarningsAsErrors`/`MSBuildTreatWarningsAsErrors`
      where appropriate) for the test tree too; confirm `dev build` + CI stay green.

## Notes

Do this in the order above — you can't enforce what you can't see. Surfacing the output (#1) is the
small enabling change; the warning cleanup (#2) is the long tail.
