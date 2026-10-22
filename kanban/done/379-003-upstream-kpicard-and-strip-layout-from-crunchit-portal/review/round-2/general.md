# Round 2 — general
**Date:** 2026-10-22
**Scope reviewed:** fix delta `0353f818e..b59530c9f` (source and tests): `KpiCard.razor`,
`KpiStrip.razor`, the deleted `components/splatted-class.cs`, `kpi-card-render-tests.cs`,
`kpi-strip-render-tests.cs`.

## Summary

The fix pass deletes the `SplattedClass` helper. `KpiCard` and `KpiStrip` now take a
`[Parameter] public string? Class` and render it on the root the way `Card` does
(`class="twe-card @Class"`). Blazor matches parameter names case-insensitively, so a caller's
`class="…"` binds to `Class` and never reaches `AdditionalAttributes`. The root class therefore
cannot be replaced by the splat. `ToneClass` now yields only the modifier (or null), and
`twe-kpi` is literal in the markup. The strip test now pins the child between the end of the
root's opening tag and the `</div>` that ends the markup, so it no longer passes trivially.
Both Design regions (component and test) were updated to match. No remaining reference to
`SplattedClass` exists under `source/`, `tests/`, or `skills/`.

Re-run on this worktree: `dotnet test -c Debug -- --filter-class KpiCard_Should_` 7/7 and
`--filter-class KpiStrip_Should_` 2/2. The test project build compiles web-spa with warnings as
errors.

## Prior findings

- M1 (suggestion): fixed. Helper is gone; `Class` parameter mirrors `Card`; passthrough tests
  with `["class"] = "caller-mark"` still assert `twe-kpi` / `twe-kpi-strip` and the caller class on
  the root.
- M2 (nit): fixed. `closeAt.ShouldBe(markup.Length - "</div>".Length)` ties the first `</div>`
  after the opening tag to the root's closing tag, and the child is asserted within that slice.

## Issues

None new on the fix delta.
