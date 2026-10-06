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

- [x] Azure AKS-vs-ACA comparison + recommendation written; Steve decided **A** (2026-10-07)
- [x] Skill section(s) written (public-safe)
- [x] AppHost Design region points to the skill
- [x] Gates: `ganda repo audit`, plus the skill-spec lint (CI)
- [x] Implementation review (disposition: clean)
- [ ] Host `open-pr`

## Azure decision — DECIDED: A (Steve, 2026-10-07)

**Decision:** A — an existing AKS cluster is the template's Azure target, through the Kubernetes
target. No Azure-specific AppHost wiring in this task. B is rejected.
C (Azure Container Apps + Flexible Server) is filed as child **070-007**, with the crunchit repo as
its reference implementation. Manual `dev deploy` / `dev deprovision` is filed as **070-006**.

**Skill follow-through for this task:**
- In `skills/tw-deploy`, state that deploying is a deliberate, operator-run action and is never
  automated in CI. CI only publishes and runs the safety checks.
- Name ACA as the Azure option for small or idle apps, "planned", without documenting commands that
  do not exist yet. 070-006 and 070-007 extend the skill when they land.
- Do not document `dev deploy` until 070-006 ships. Use `aspire deploy` directly for now.

### Comparison (kept for the record)

Sources: aspire.dev `deploy-to-azure-kubernetes-service-aks`, `azure-kubernetes-service-aks-integration`,
`configure-ingress-on-aks`, `deploy-to-azure-container-apps`, `configure-azure-container-apps-environments`,
`deploy-to-azure-container-apps-sandboxes`, `persistent-volumes-on-kubernetes`, `external-parameters`.

There are three Azure options:

- **A. Existing AKS cluster as a plain Kubernetes target.** This needs no AppHost change.
  - Point `aspire deploy -- --Publish:Target=kubernetes` at the AKS kubectl context.
  - ACR serves as `registry-endpoint`.
  - `ingress-class` is set to the cluster's controller.
- **B. `AddAzureKubernetesEnvironment`** (`Aspire.Hosting.Azure.Kubernetes`). `aspire deploy`
  provisions the AKS cluster, ACR and pull identity, then installs the generated Helm chart. The
  docs give no `AsExisting` for AKS, so this option owns the cluster: `aspire destroy` deletes it.
- **C. `AddAzureContainerAppEnvironment`** (`Aspire.Hosting.Azure.AppContainers`). Aspire emits
  ACA Bicep and provisions it.

| | A. AKS, existing cluster | B. AKS, provisioned by Aspire | C. Azure Container Apps |
|---|---|---|---|
| **Lock-in** | None. It is the same Helm chart as on-prem, and the only Azure-specific values are the context, registry and ingress class. | Medium. The chart stays portable, but cluster provisioning (Bicep, node pools, subnets) lives in the app's AppHost. | High. The output is ACA Bicep, there is no Kubernetes artifact, and leaving Azure means a different target. |
| **AppHost change** | none | a new `Publish:Target` value, its own environment and safety suite | a new `Publish:Target` value, its own environment and safety suite |
| **Cost model** | You pay for the cluster's node VMs whether or not it is busy. The cost can be shared across many apps on one cluster. | A dedicated cluster per app costs a node-VM floor for each app. This is the worst fit for small apps. | Consumption profile, billed per use. Express mode can scale to zero. This is the cheapest for small or idle apps. The docs give no cost figures. |
| **Ingress** | Our decision (a) holds as-is: the cluster controller sends traffic to YARP. The controller can be nginx, or AGC with cert-manager via the Ingress API. | The documented path is AGC with Gateway API. It needs subscription preview features (`ManagedGatewayAPIPreview`, `ApplicationLoadBalancerPreview`) and dedicated subnets of /24 or larger. YARP can be the single route target. | Built-in Envoy HTTPS ingress, so no controller to run. `WithExternalHttpEndpoints` on YARP only keeps "only the ingress is external". There is a managed custom-domain certificate. YARP's http hop to web-server stays internal. |
| **Secrets** | Same as on-prem: `values.yaml` `secrets.*` become K8s Secrets. Key Vault needs the CSI driver, set up as cluster infrastructure. | K8s Secrets via the chart. The docs do not say secret parameters go to Key Vault. | ACA secrets, with the managed identity created by Aspire. The docs are silent on mapping secret parameters to Key Vault. |
| **Postgres** | A PVC on the cluster's default storage class (Azure managed disk). The same StatefulSet and same manual `kubectl exec` migration as on-prem. | Same as A. | Container volumes become **Azure Files (SMB) shares**, which are a poor and risky home for Postgres data. The docs recommend managed services for production. That means an Azure Postgres Flexible Server resource: a second Azure-specific resource and a different migration path. |
| **Maturity** | Uses only what already ships and is tested (`kubernetes-publish-tests`). | The AKS environment is new. AGC features are preview, and `AddPersistentVolume` on AKS is `ASPIRECOMPUTE002`. | The core API is stable. Express mode is experimental (`ASPIREACAEXPRESS001`). |

**ACA Sandboxes are out of scope** for hosting this template. They are prerelease, with no
volumes, no TCP and no private service discovery, and they are built for isolated, auto-suspending
workloads (agent or code sandboxes), not a stateful multi-service app.

**Recommendation: A** — AKS is the Kubernetes target, with no Azure-specific AppHost wiring.

- It keeps the parent goal of "no Azure lock-in" literally true: one chart, one production-safety
  suite, one migration story.
- The skill already documents it (`skills/tw-deploy` → Azure).
- **Do not add B.** It ties cluster lifecycle to the app (`aspire destroy` deletes the cluster),
  and its ingress path depends on preview features.
- **Revisit C only as a separate child**, and only when a small or idle tenant is the real cost
  driver. If it is taken up, it brings in Azure Postgres Flexible Server and a separate safety
  suite. It is a second, Azure-only deploy story, not a replacement.

**Decided A (2026-10-07).** No AppHost wiring follows; the skill already says so. B and C
would each need a new child under 070 (C is filed as 070-007).

## Notes

- Lands after 070-003 and 070-004, so the guidance describes what exists.

## Results

- **Owning skill: new `skills/tw-deploy/SKILL.md`.** None of the eight existing skills covers
  deployment: they cover domain, slice, contracts and Blazor topics. It is public-safe, with
  rules and reasoning only, and no history, task numbers or client names. It ships in generated
  apps through the existing `skills/**` pack glob. It covers:
  - the target matrix;
  - `dev publish` / `aspire publish` / `aspire deploy` for each target, plus Kubernetes
    prerequisites;
  - the production-safety table and the rules for new publish-mode code;
  - secrets and parameters (`.env` and `values.yaml`, the two-key Postgres password under plain
    helm);
  - Postgres storage and the exact migration commands for each target;
  - the ingress topology (YARP stays the router, the chart installs no controller);
  - Azure (an existing AKS cluster is a Kubernetes target);
  - container-runtime neutrality (`ASPIRE_CONTAINER_RUNTIME`, never hard-code `docker`).
- **AppHost Design region:** it now points to the skill as the operator map, and says the two are
  kept in sync.
- **Ripple from the new shipped skill:** the `dev template-smoke` `AssertSkillsShipped` list now
  includes `tw-deploy/SKILL.md`, and the "eight" → "nine" wording changed in that harness and in
  `AGENTS.md`.
- **Azure:** Steve decided **A** (existing AKS through the Kubernetes target). No Azure AppHost
  wiring was added. Decision follow-through in `skills/tw-deploy`:
  - Publishing and deploying: deploying is a deliberate, operator-run action, never automated in
    CI; CI only publishes and runs the safety suites. `aspire deploy` is used directly (no
    `dev deploy` until 070-006 ships).
  - Azure: ACA is named as the planned option for small or idle apps, with no commands documented;
    `AddAzureKubernetesEnvironment` (B) is ruled out with its reasons; Postgres on ACA volumes is
    ruled out. 070-006 / 070-007 extend the skill when they land.

**Gates:**
- `vally lint skills`: 9/9 passed.
- dev-cli runfile build: clean.
- aspire-app-host build (Release): 0 warnings, 0 errors.
- `ganda repo audit`: passed.
- No `dev run` or deploy was done, and `dev template-smoke` was not run locally. The harness edit
  only adds one filename to a list; CI template-smoke covers it.

**Implementation review:**
- 1 round at effort 2. Reviewers: general (a subagent that fact-checked against the repo) and plan_alignment.
- Final counts: bug 0, suggestion 0, nit 1 fixed, 0 open, 0 wontfix.
- Disposition: **clean**. M1, the stale "If he picks…" wording in task.md, is fixed.
- Paths: `review/review-framework.md`, `review/round-1/{general,plan-alignment,merged}.md`, `review/disposition.md`.

### How to validate

**Smoke:**

```bash
vally lint skills
dev template-smoke              # generated app must contain skills/tw-deploy/SKILL.md
grep -n "skills/tw-deploy" source/container-apps/aspire/projects/aspire-app-host/program.cs
```

**Expect:**
- `skills/tw-deploy/SKILL.md` states deploys are operator-run and never automated in CI, names
  ACA as planned with no commands, and does not mention `dev deploy`.
- `vally lint` reports "9 skill(s) linted, 9 passed".
- template-smoke reports "Generated app contains skills/ (nine SKILL.md files; analysis/ excluded)."
- The AppHost Design region names `skills/tw-deploy/SKILL.md`.
- Reading the skill, its commands match the AppHost: parameter names from `constants.cs`,
  `Publish:Target` values `compose`/`kubernetes`, and migration commands identical to the Design
  region.

## Session
- 2026-10-07: Steve decided A; C filed as 070-007, manual dev deploy as 070-006. Resume: finish skill follow-through, review, open PR.
- 2026-10-07: skill follow-through for decision A done (operator-run deploys, ACA planned, B ruled out).
- 2026-10-07: implementation review round 1 (effort 2: general + plan_alignment), disposition clean; general subagent af0742154f2eab428.

- Created: 2026-10-03 (rewrite of 070)
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-06T17:31:22Z

## Blocked (resolved 2026-10-07 — Steve decided A)

2026-10-06: Steve's Azure target decision (A: existing AKS via the Kubernetes target, recommended; B: AddAzureKubernetesEnvironment; C: Azure Container Apps) — comparison in 070-005 task.md "Azure decision" section
