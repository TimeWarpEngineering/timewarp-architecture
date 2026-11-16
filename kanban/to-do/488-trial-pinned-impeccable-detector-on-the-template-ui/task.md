# Trial pinned impeccable detector on the template UI

## Description

Research only, small and time-boxed. Run a pinned exact npm version of `npx impeccable detect` ([pbakaus/impeccable](https://github.com/pbakaus/impeccable), Apache-2.0) against the Architecture template UI in two ways, record hits, misses and false positives, then recommend whether to add a CLI-only CI detect step (B), fold the rules that actually fired into `tw-blazor-css-strategy` (D), or neither.

Source: [Impeccable eval](https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-11-impeccable.md) ([timewarp-intel PR #121](https://github.com/TimeWarpEngineering/timewarp-intel/pull/121)). Busara verdict: REWORK (med). C is GREEN and folds in B, K and L. The assumption most likely to fail is that FluentUI shadow DOM or tokens flood the scan with false positives.

Filed 2026-11-11 at Steven's request ("Yes, create the Impeccable detector trial task in timewarp-architecture."). Not launched.

## Requirements

1. **Pin an exact npm version (K, folded).** Use a pinned `npx impeccable@<exact-version> detect`. Record the version in Results. Do not float to latest.
2. **Rendered URL scan.** Start the template and run detect against the running page URL. The rules that need rendering (low contrast, tiny text) only run on a URL scan. Blazor renders to plain HTML and CSS, so this scan avoids the detector's lack of a built-in `.razor` type.
3. **Source scan.** Also run detect against `.razor` / `.razor.css` sources via `detector.extensions`.
4. **FluentUI and brand ignores (L, folded).** Add ignore config for FluentUI web components and design tokens, plus the brand typography, so brand fonts and FluentUI internals are not reported as slop.
5. **Triage (~1 hour).** Record hits, misses and false positives. Spend about an hour sorting them. Note anything hidden by shadow DOM.
6. **Recommend, do not implement, B and D.** From the triage, recommend whether to add (B) a CLI-only CI detect step and (D) the rule ideas that actually fired into the `tw-blazor-css-strategy` skill, or neither. Do not add the CI step or edit the skill in this task.

## Out of scope

Busara gave all of these a KILL:

- The Impeccable skill pack (A)
- Agent hooks (E)
- Live mode (F)
- A Chrome extension (I)
- Adopting PRODUCT.md / DESIGN.md (G)
- Cross-linking paperwork to the C# anti-slop eval (H)
- Lifting the command vocabulary into `tw-blazor*` skills (J), unless this trial later shows uncovered categories

Also out of scope: adding a CI detect step or editing `tw-blazor-css-strategy` in this task. Those are follow-ups only if the trial shows a useful signal-to-noise ratio.

## Checklist

- [ ] Pin an exact npm version and record it
- [ ] Scan the running template URL
- [ ] Scan `.razor` / `.razor.css` via `detector.extensions`
- [ ] Add FluentUI, token and brand-typography ignores
- [ ] Record hits, misses and false positives (~1 hour triage)
- [ ] Recommend CI detect (B), rule borrow into `tw-blazor-css-strategy` (D), or neither — do not implement those follow-ups here

## Session

- Created: 3075946 (2026-11-11)

## Notes

- Detector: https://github.com/pbakaus/impeccable (Apache-2.0)
- Eval: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-11-impeccable.md
- Intel PR: https://github.com/TimeWarpEngineering/timewarp-intel/pull/121
- Filed only; not launched. Do not start until Steven says so.
