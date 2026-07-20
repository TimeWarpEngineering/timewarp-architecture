# Migrate web-server endpoints from MVC shims to generated FastEndpoints

## Description

Finish the FastEndpoints migration that task 004 started and closed for api-server only
(closeout 2026-06-26: api endpoints generated from contracts via `FastEndpointSourceGenerator`).
No task ever tracked the web half, no ADR records "web stays MVC" as a decision, and AGENTS.md
line 30 merely describes the split — the most consistent reading of the record is that the
migration stopped at api and web-server fell through the cracks. Meanwhile web-server has
accumulated **19 hand-written MVC `BaseEndpoint<TRequest, TResponse>` shims — 10 of them added in
one week by the 104 identity tasks** — and every future slice (104-005, 104-016, …) adds more.

Generation removes the drift class that TWA0005/0006 exist to guard (endpoint verb vs `[ApiRoute]`;
missing endpoints), collapses the dual serializer-pipeline maintenance (the string-enum seam change
in task 108 had to patch MVC JsonOptions AND HttpJsonOptions precisely because of the split), and
unifies the endpoint story the template teaches.

## Requirements

- **Generate web-server endpoints from web-contracts** the way api-server generates from
  api-contracts. Reconcile the attribute vocabularies: api-contracts drive the generator with
  `[ApiEndpoint]`; web-contracts carry `[ApiRoute]` (+ `[AuthApiRequest]`, `[OpenDataQueryParameters]`).
  Either teach the generator the `[ApiRoute]` shape or annotate web-contracts with the generator's
  attribute — pick during planning; prefer whichever leaves ONE convention for both servers.
- **Auth metadata passthrough** — the real design wrinkle. Generated endpoints must express what the
  hand shims do today: `[Authorize(Policy = AgentTokenDefaults.IdentityReadPolicy)]` on
  get-agent-identity; anonymous ceremony endpoints; anything `[AuthApiRequest]` implies. The
  contract must become the single source of auth intent (attribute on the operation → generated
  endpoint attribute), not a hand-maintained sidecar.
- **Delete all 19 shims** under `web-server/features/**` plus the MVC controller wiring
  (`BaseEndpoint`, AddControllers/MapControllers as applicable). Blazor hosting coexists with
  FastEndpoints — verify pipeline order against the identity cookie/bearer schemes and
  UseAuthentication/UseAuthorization placement (104-003/004 wiring must keep working).
- **Routes must not change.** The 53 web-server integration tests (incl. the well-tested identity
  surface: 12 passkey + 19 agent + options-binding) are the migration safety net — they must pass
  unmodified except where they assert MVC-specific implementation detail.
- **Serialization**: FastEndpoints rides `ConfigureHttpJsonOptions`, already covered by
  `ContractSerializationDefaults.Apply` (task 108). Add/keep an integration assertion that the
  string-enum wire shape holds through the generated endpoints (this also closes the 108 review's
  api-server loose end: verify FastEndpoints emits PascalCase string enums).
- **TWA0005/0006** must recognize generated endpoints as satisfying verb-match and coverage
  (confirm how 085-001 handles the api side; extend if the detection is api-attribute-specific).
- **FluentValidationBehavior stays the validation path** (mediator pipeline) — do not adopt
  FastEndpoints' own validator integration; handlers do not re-validate (Definition of Done).
- Update AGENTS.md line 30 (endpoint story becomes uniform), the web-api-contracts skill if shim
  references exist, and any docs describing BaseEndpoint.
- Package/flag hygiene: FastEndpoints reference lands in web-server behind existing conventions;
  no new template flags; TWA0008/0010 respected near any conditional regions.

## Checklist

- [ ] Plan: attribute-vocabulary decision ([ApiRoute] vs [ApiEndpoint] unification) + auth-passthrough design
- [ ] Generator support for web-contracts operations incl. auth metadata
- [ ] Generated endpoints for all 19 operations; shims + MVC wiring deleted
- [ ] Identity auth flows verified: cookie session, bearer policy 401/403, scheme isolation
- [ ] Integration suite passes with routes unchanged; string-enum wire assertion through FastEndpoints
- [ ] TWA0005/0006 coverage semantics confirmed/extended for generated web endpoints
- [ ] AGENTS.md + skill/doc updates; Design regions reconciled
- [ ] dev build 0/0; full dev test

## Notes

- Origin: 2026-07-20 review discussion — surfaced while reviewing task 108's dual-pipeline
  serializer patch. Evidence trail: kanban/done/004-migrate-api-to-fastendpoints.md (api-only
  closeout), no web migration task in any column, no ADR on the split.
- Consider writing the missing ADR as part of this task ("endpoints are generated FastEndpoints
  from contracts, both servers") so the decision is recorded this time.
- Sizeable task — likely wants sub-tasks at plan time (generator work vs migration vs analyzer).
  The identity endpoints being freshly review-hardened and integration-tested makes NOW the
  cheapest time: the safety net is at its strongest.

## Session

- Created: 2026-07-20
