# Deploy guidance in a skill for Compose, Kubernetes and Azure targets

## Description

Child of 070. Write the deploy story into a **skill**, not a docs tree: `documentation/` is
retired and skills plus Design regions are the record. Skills ship in generated apps and are
public, so write rules and reasoning only, with no history or client names. Also decide the
**Azure** target.

## Requirements

1. **Azure decision.** Compare AKS (reuse the Helm chart from 070-004 via `aspire deploy`) against
   Azure Container Apps (Aspire's dedicated target, including ACA Sandboxes in 13.6) for this
   template. Cover lock-in, cost model, ingress, and secrets. **Write the comparison and a
   recommendation, then stop for Steve's decision.** Implement the chosen target's AppHost wiring
   only after he decides, possibly as a further child.
2. **Skill content.** Pick the owning skill: extend an existing repo skill under `skills/` or add a
   new one if none fits. Record the choice. It covers:
   - the target matrix;
   - how to run `aspire publish` and `aspire deploy` per target;
   - the production-safety rules (what never ships to production and why);
   - secrets and parameters;
   - Postgres migrations per target;
   - the ingress topology decision from 070-004;
   - container-runtime neutrality (Docker, Podman, WSL containers via task 277).
3. Point from the AppHost Design region to the skill. No new `documentation/` or `devops/`
   README.

## Checklist

- [ ] Azure AKS-vs-ACA comparison + recommendation written; **stop for Steve's decision**
- [ ] Skill section(s) written (public-safe)
- [ ] AppHost Design region points to the skill
- [ ] Gates: `ganda repo audit`, plus the skill-spec lint (CI)
- [ ] Implementation review; host `open-pr`

## Notes

- Lands after 070-003 and 070-004, so the guidance describes what exists.

## Session

- Created: 2026-10-03 (rewrite of 070)
