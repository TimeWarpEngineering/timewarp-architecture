# Disposition — task 131 full-repo code review

**Date:** 2026-07-28
**Steward:** Steven T. Cramer (session with Grok verification + decision walk)
**Status:** decisions complete — implementation next

**Reviewer inputs:**
- Primary findings: `findings.md` (Kimi K3, F-001…F-017)
- Verification: `review/round-1/claude-verification.md`
- Verification: `review/round-1/grok-verification.md` (+ delta vs Claude)

**Outcome shape:** mixed — some fixes under this task, some accepted follow-on
child tasks, some tracked on existing 104-xxx work. No finding rejected.

---

## Decision summary

| ID | Severity | Decision | Where work lands |
|----|----------|----------|------------------|
| F-001 | blocker | **Accept expanded** | **131 implement** |
| F-002 | major | **Accept full delete + retire TWA0005** | **131 implement** |
| F-003 | major | **Accept** | Generator-hardening **follow-on** |
| F-004 | major | **Accept** | Same generator-hardening **follow-on** |
| F-005 | major | **Accept delete** (not implement EndpointType) | Same generator-hardening **follow-on** |
| F-006 | major | **Accept** | Identity de-dup **follow-on** |
| F-007 | major | **Accept** | Gate/tooling SSOT **follow-on** |
| F-008 | minor | **Accept** | Same generator-hardening **follow-on** |
| F-009 | minor | **Accept corrected** (Debug/Release flip) | **131 interim** + posture **104-021** |
| F-010 | minor | **Accept split** | **131** fossils/link; Passwordless/B2C **104-016/021** |
| F-011 | minor | **Accept** | **131 implement** |
| F-012 | minor | **Accept** (delete dead detection only) | **131 implement** |
| F-013 | minor | **Accept** | **131 implement** |
| F-014 | minor | **Accept expanded** | Same generator-hardening **follow-on** |
| F-015 | minor | **Accept split** | **131** SPA catch/verbs; shared core **follow-on** |
| F-016 | note | **Accept narrowed** — document substrate | **131** docs (AGENTS + placement skill) |
| F-017 | note | **Accept** full residue sweep | **131 implement** |

---

## Per-finding dispositions

### F-001 — Accept expanded (blocker)

**Decision:** Delete `ConfigureAzureAppConfig` entirely (method, call site, Azure package
references on foundation-server), not merely the three `Console.WriteLine`s.

**Rationale:** Secret echo is the symptom; config-source composition, hard-coded
Sentinel/refresh/Key Vault opinions, and sole-consumer Azure.Identity /
AppConfiguration package weight do not belong in the published foundation library.
Hosts that want App Config add it in their own bootstrap.

**Implements under:** 131

---

### F-002 — Accept full delete + retire TWA0005 (major)

**Decision:** Delete `base-endpoint.cs` (MVC `BaseEndpoint`); retire **TWA0005** (reserve
ID; do not reuse); keep **TWA0006** scoped on `BaseFastEndpoint`; remove stale ISender
TODOs and dual-maintenance Design lines on the FastEndpoint bridge; remove
`Mvc.JsonOptions` configuration; update analyzer tests, AGENTS.md TWA table, and related
comments.

**Rationale:** Template migrated MVC → FastEndpoints. No product subclasses; hosts never
`AddControllers`/`MapControllers` — bridge is unusable. No reason to deviate back to MVC
for API ingress; reintroduce only via a deliberate future decision if product ever requires
it. Pre-adoption is the cheapest time to drop the public type.

**Implements under:** 131

---

### F-003 — Accept → generator-hardening follow-on (major)

**Decision:** Delete static `RouteRegistry`; per-compilation `.Collect()` + in-batch
duplicate reporting. Include Claude expansion (IDE incremental self-conflict).

**Implements under:** child task (theme B — generator/analyzer hardening)

---

### F-004 — Accept → same generator-hardening follow-on (major)

**Decision:** Shared hosted-route discovery as **linked source** into both analyzer
packages (flag-parameterized for intentional rule differences); fix live
`[ApiEndpoint]`+`[ClientOnlyContract]` contradiction; delete duplicate `GetAllNamespaces`.

**Implements under:** same child as F-003

---

### F-005 — Accept delete EndpointType → same follow-on (major)

**Decision:** Delete `ApiEndpointAttribute.EndpointType` and dead extraction/emission;
fix docs that teach it (`ApiEndpointSourceGenerator.md`). Do **not** implement the
override (YAGNI; zero consumers; generic base shape unspecified).

**Implements under:** same child as F-003

---

### F-006 — Accept → identity de-dup follow-on (major)

**Decision:** Shared `IdentityProblems` factories (parameterize intentional wording
variants); ceremony-preamble helpers per family where ladders match; **do not** merge
handlers. Security-critical ordering moves from convention-by-comment to one path.

**Implements under:** child task (theme C — identity exemplar)

---

### F-007 — Accept → gate/tooling follow-on (major)

**Decision:** Shared smoke harness; all rewrite/suffix lists derived from
`msbuild/timewarp-platform-packages.props` SSOT (126-006 style); port namespace-literal
scan to publish-smoke; use `IsBinObjOrArtifacts` consistently.

**Implements under:** child task (theme D — gate/tooling)

---

### F-008 — Accept → same generator-hardening follow-on (minor)

**Decision:** Fail closed on unrecognized `HttpVerb` (no `_ => "Get"`); cover
`ResolveHttpVerbName` fallback and existing Head/Options enum members.

**Implements under:** same child as F-003

---

### F-009 — Accept corrected (minor) — interim under 131 + 104-021

**Decision (corrected mechanics):** `MOCK_AUTHENTICATION` is **Debug-only**, not
unconditional. Release / template-smoke / `dotnet publish` compile the MSAL/`AzureAdB2C`
branch against placeholder config — Debug↔Release auth flip.

**Interim under 131:** Make mock authentication consistent across configurations (define
in all configs, or equivalent so Debug and Release agree) and fix the contradictory
csproj comment (“Uncomment if you want Mock B2C” above an always-on-in-Debug line).

**Posture (Entra non-default, MSAL path, AzureAd appsettings residue):** remains
**104-021**.

**Implements under:** 131 (interim) + 104-021 (long-term)

---

### F-010 — Accept split (minor)

**Under 131:**
- Remove B2C/PWA fossil `<!--#if -->` blocks / dead TODOs where safe
- Fix HomePage MediatR link → TimeWarp.Mediator (or drop the line)
- Clear web-server program.cs stale TODO / commented RazorPages/ServerSideBlazor lines

**Tracked (confirm coverage on disposition implement):**
- Passwordless CDN script + hardcoded TimeWarp tenant public key in `App.razor` (+ related
  appsettings / `passwordless-service` Console.WriteLine of ApiKey) — **104-016 / 104-021**.
  Extend those task specs if they do not already require key/CDN removal from template
  output.

**Implements under:** 131 + 104-016/021

---

### F-011 — Accept (minor)

**Decision:** Replace five discrete `platform/postgres/*` excludes with
`source/container-apps/web/platform/postgres/**`. Keep
`ef-principal-store-infrastructure.cs` and `web-infrastructure-tests/**` entries.

**Implements under:** 131

---

### F-012 — Accept (minor)

**Decision:** Delete the redundant `UseAnalyzerPackages` re-detection PropertyGroup in
`tests/Directory.Build.props`. Keep analyzer wiring ItemGroups (source DBP does not reach
tests). Full shared-props extract optional later with F-007 theme.

**Implements under:** 131

---

### F-013 — Accept (minor)

**Decision:** Collapse identical ternary; drop redundant path `Contains` arm; delete
`TryFindIncompleteMultiSegmentFunction` until a multi-segment function is registered
(YAGNI).

**Implements under:** 131

---

### F-014 — Accept expanded → generator-hardening follow-on (minor)

**Decision:** Document TWE/SG in AGENTS.md; consolidate TWE registry (or drop false
authority claim); wire-or-delete unused TWE001/002/004; dedupe SG001 declarations.
Renumber into TWA optional later — not required for this disposition.

**Implements under:** same child as F-003

---

### F-015 — Accept split (minor)

**Under 131:** Align SPA `BaseApiService` with the better test-side behavior — narrow
`HandleProblemResponse` catch; consistent `NotSupportedException` / verb matrix (drop or
support Head/Options consistently).

**Follow-on:** Shared transport core both SPA and TestApiService compose.

**Implements under:** 131 (quick fix) + transport **follow-on**

---

### F-016 — Accept narrowed — document (note)

**Decision:** Placement is intentional (Design regions already state Features substrate for
TWA0009). Document the tier (name, litmus, ModuleIds/RoleIds examples) in AGENTS.md and
the feature-placement skill. No mandatory file moves now (104-021 may rehome
authorization constants later).

**Implements under:** 131 docs

---

### F-017 — Accept full sweep (note)

**Decision:** One cleanup pass under 131:
- `GenericPipelineBehavior` — stop teaching `Console.WriteLine`; prefer `ILogger` exemplar
  and/or relocate per placement litmus (artifact folder should not hold non-bootstrap logic)
- Delete `web/features/v2/overview.md` (fold sentence into a how-to if needed)
- Delete dead `ExampleConst` / commented leftover in testing constants
- Merge aspire comment-only `#if web` with the adjacent real block
- Sync `feature-membership.targets` comment (web vs api/grpc “trees may be absent”)
- Fix AGENTS.md: api `platform/` is **absent**, not “empty (no content yet)”

**Implements under:** 131

---

## Follow-on tasks to create (after / with 131 implement)

Use `ganda kanban create` (never hand-number). Suggested titles:

1. **Generator/analyzer hardening** — F-003, F-004, F-005, F-008, F-014  
2. **Collapse identity handler problem-factory and ceremony-preamble duplication** — F-006  
3. **Extract shared template-smoke harness; derive rewrite-scan suffixes from props SSOT** — F-007  
4. **Share API transport core between SPA BaseApiService and TestApiService** — F-015 (extract half)

Confirm/extend on existing tasks:
- **104-021** — Entra/MSAL posture; F-009 long-term; F-010 B2C/PWA posture; bare Features rehome if desired  
- **104-016** — Passwordless CDN + tenant key removal from template (with 104-021)

---

## Implement under 131 (checklist)

- [x] F-001 — remove Azure App Config module path from foundation-server  
- [ ] F-002 — delete MVC BaseEndpoint; retire TWA0005; Mvc.JsonOptions; tests; AGENTS  
- [ ] F-009 — mock auth consistent across configurations + csproj comment  
- [ ] F-010 — chrome fossils + MediatR link (not Passwordless key unless specs already allow)  
- [ ] F-011 — postgres exclude glob  
- [ ] F-012 — tests DBP dead detection  
- [ ] F-013 — grammar analyzer simplify  
- [ ] F-015 — BaseApiService catch/verb alignment  
- [ ] F-016 — document Features substrate  
- [ ] F-017 — residue sweep  
- [ ] Create follow-on kanban tasks for B/C/D themes  
- [ ] Confirm 104-016/021 cover Passwordless key/CDN  
- [ ] Commit review artifacts + fixes; Results on task 131  

**Not under 131 code:** F-003, F-004, F-005, F-006, F-007, F-008, F-014 (follow-ons).
