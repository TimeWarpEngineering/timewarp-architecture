# Multi-model review (Grok 4.7 + Fable) of published OpenAPI at arch.timewarp.work

## Description

Two models review the **published** OpenAPI document of the deployed reference app, each on its
own: **Grok 4.7** and **Fable** (`claude-fable-5-1`). Then the findings are merged, verified, and
dispositioned. The target is what ships to consumers (humans, generated clients, and agents). It is
not the source diff.

- Document: https://arch.timewarp.work/openapi/v1.json (OpenAPI 3.1.1, `Web.Server API v1`,
  62 paths at task creation)
- Rendered: https://arch.timewarp.work/scalar/v1 · index: https://arch.timewarp.work/llms.txt
- Excluded routes are listed in `info.description` and `x-timewarp-excluded-routes`
  (`/.well-known/*` discovery documents)

This is the durable home for the snapshot, the two reviews, the merged findings, and the
disposition. Fixes go to follow-on tasks.

## Requirements

### Snapshot first

- Save the document to `review/openapi-v1.snapshot.json` with the fetch time and the deployed
  version or commit if it is exposed. Both reviewers read the **same snapshot** so their findings
  can be compared.

### Independent reviews (no anchoring)

- Grok 4.7 and Fable each get the same `review/review-brief.md` and write
  `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Neither reviewer sees the other's output until both files are committed.
- Reviewers may read the repo source (contracts under `source/`, `tw-web-api-contracts`, the
  OpenAPI wiring in web-server) to explain a finding. The review is still of the published document.
- Live probing: unauthenticated `GET`s only (health, discovery, the doc itself). No `POST`s, no
  paid routes (`/api/tip`, `/api`, `/api/demo/metered-capability`), no identity ceremonies.

### Review lenses (in the brief)

1. **Validity**: OpenAPI 3.1.1 / JSON Schema 2020-12 conformance. Run a linter (e.g. Spectral
   or Redocly) and attach its output.
2. **Consistency**: path casing and prefixes (e.g. `/Analytics/TrackEvent` vs `/api/...`,
   `/api/Feedback` vs `/api/admin/feedback`), operationId and tag naming, and parameter naming.
3. **Contracts**: request and response schemas, `OneOf<Response, SharedProblemDetails>` shown as
   problem details, status codes (including the 402/503 rules in `info.description`), nullability,
   enums, and examples.
4. **Security**: security schemes, per-operation requirements, admin vs user vs agent routes, and
   anything leaked by the doc.
5. **Agent and generated-client consumability**: descriptions, summaries, the
   `[Operation]`-derived metadata, and whether a client generator (e.g. Kiota or NSwag) produces
   sane output.
6. **Doc vs reality**: spot-check that documented free `GET` routes behave as documented, and that
   the excluded-routes list matches what is served.

Each finding has a severity (blocker / major / minor / nit), a JSON pointer into the snapshot,
evidence, and a proposed fix that names the source owner (contract, generator, transformer, or
skill).

### Merge, verify, disposition

- `review/findings.md`: deduplicated union of both reviews. Record which model found what, and
  mark findings both models agree on.
- Verify each finding against the snapshot or the source before accepting it (do not rely on the
  reviewer's own report).
- `disposition.md`: accept / reject (with reason) / defer per finding. Each accepted finding links
  a follow-on kanban task (created via `ganda kanban reserve`/`claim`, never hand-numbered).
- Prefer fixes that generate the doc from contracts or add a build check over hand-patched doc
  text (AGENTS.md: "generate one from the other or add a build check").

## Checklist

- [ ] Snapshot the published document into `review/`
- [ ] Write `review/review-brief.md` (lenses, severity scale, output format, probing rules)
- [ ] Run linter, attach output
- [ ] Grok 4.7 review → `review/round-1/grok-4.7.md`
- [ ] Fable review → `review/round-1/fable.md`
- [ ] Merge + verify → `review/findings.md`
- [ ] Disposition with follow-on task links → `disposition.md`

## Notes

- Skills: `tw-implementation-review` (review/ folder layout, merge → disposition on the same host
  task; no sibling apply-findings task), `tw-agent-collaboration`, `tw-web-api-contracts`.
- Precedent: `kanban/done/131-complete-repo-code-review-by-kimi-k3/`.
- Related: 257 (Microsoft.OpenApi 3.x), 331 (outside agent reviews feedback through the API).
- Output is kanban-only (review artifacts). Product changes belong to follow-on tasks.

## Session

- Created: f542f9e6-afba-4597-8f76-ca55f50824db (2026-10-31)
