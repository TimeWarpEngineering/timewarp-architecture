# ADR accept and permission evaluator seam docs for optional external PDP

**Parent:** 182 · **Order:** E (ADR draft with 182-001; accept after 182-003+)

## Description

Publish the architectural decision and document how consumers swap `IPermissionEvaluator` for OpenFGA/Cedar without rewriting endpoints.

## Requirements

- ADR under `documentation/developer/conceptual/architectural-decision-records/` (permission-centric authz; evaluator sole seam; roles as bundles; external PDP optional).
- Consumer how-to: implement evaluator; do not require AppHost OpenFGA by default.
- Note Entra branch should migrate to same session-permissions source when touched.

## Checklist

- [x] ADR accepted
- [x] Seam / consumer docs
- [x] Results + How to validate

## Notes

### Implementation plan (Phase 2 — 2026-08-12)

Docs-only: promote draft ADR-0010 to approved; how-to for DI replace of `IPermissionEvaluator`; indexes; Design-region cross-link.

### Phase 4b

Single-reviewer effort 1 (docs-only): **Accept with nits** → nits applied (Replace snippet + fail-closed wording). Disposition: **clean**.

## Results

### Summary

- **ADR-0010** accepted at
  `documentation/developer/conceptual/architectural-decision-records/approved/0010-permission-centric-authorization.md`
  (status accepted; Decision Outcome as shipped for 182-001…004; external PDP optional; Entra
  branch note; 182-006 called out as open).
- Draft removed from `proposed/0010-…`.
- **How-to:** `documentation/developer/how-to-guides/how-to-swap-permission-evaluator-for-external-pdp.md`
  — replace `AddScoped<IPermissionEvaluator, PermissionEvaluator>()` in `web-server/program.cs`;
  leave handler + policy registration; no AppHost OpenFGA.
- Indexes: `approved/overview.md`, `how-to-guides/overview.md` (Authorization section).
- Design-region cross-link on `i-permission-evaluator-application.cs`.
- Review: Accept with nits; nits fixed before done.

### How to validate

**Smoke**

```bash
test -f documentation/developer/conceptual/architectural-decision-records/approved/0010-permission-centric-authorization.md
test ! -f documentation/developer/conceptual/architectural-decision-records/proposed/0010-permission-centric-authorization.md
test -f documentation/developer/how-to-guides/how-to-swap-permission-evaluator-for-external-pdp.md
rg -n '^\* Status: accepted' documentation/developer/conceptual/architectural-decision-records/approved/0010-permission-centric-authorization.md
rg -n '0010-permission-centric|how-to-swap-permission-evaluator' \
  documentation/developer/conceptual/architectural-decision-records/approved/overview.md \
  documentation/developer/how-to-guides/overview.md
```

**Expect**

- Status line is `* Status: accepted`.
- Approved overview lists ADR 0010; how-to overview has Authorization section with link.
- How-to documents `web-server/program.cs` `AddScoped<IPermissionEvaluator, PermissionEvaluator>()`.
- No new OpenFGA AppHost resource required by this work.

**Automated gate**

```bash
dotnet build source/container-apps/web/projects/web-application/web-application.csproj -c Debug --no-restore
# expect: 0/0 (Design-region only source touch)
```

**Not in scope:** running OpenFGA/Cedar; Entra code migration; 182-006 agent scopes.

## Session

- Orchestrator: Grok tw-orchestrate-task 182-005 (2026-08-12) — plan + implement + review + done
