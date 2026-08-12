# ADR accept and permission evaluator seam docs for optional external PDP

**Parent:** 182 · **Order:** E (ADR draft with 182-001; accept after 182-003+)

## Description

Publish the architectural decision and document how consumers swap `IPermissionEvaluator` for OpenFGA/Cedar without rewriting endpoints.

## Requirements

- ADR under `documentation/developer/conceptual/architectural-decision-records/` (permission-centric authz; evaluator sole seam; roles as bundles; external PDP optional).
- Consumer how-to: implement evaluator; do not require AppHost OpenFGA by default.
- Note Entra branch should migrate to same session-permissions source when touched.

## Checklist

- [ ] ADR accepted
- [ ] Seam / consumer docs
- [ ] Results + How to validate
