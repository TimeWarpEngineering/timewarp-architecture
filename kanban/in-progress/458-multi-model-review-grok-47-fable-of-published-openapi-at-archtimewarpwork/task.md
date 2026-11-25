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

## Goal (implementer restatement)

Review the OpenAPI document that the live reference app actually serves at
`https://arch.timewarp.work/openapi/v1.json`, not the code diff that produced it. Two models
review one frozen copy of that document independently, and the result is a verified, deduplicated
list of problems, each with a decision and, when accepted, its own follow-on task.

Acceptance criteria:

1. `review/openapi-v1.snapshot.json` holds the fetched document, and a sibling metadata file
   records the fetch time and whatever deployed version is exposed (commit if any).
2. `review/review-brief.md` states the six lenses, the severity scale, the finding format (severity,
   JSON pointer, evidence, fix with source owner), and the probing rules (unauthenticated GETs only,
   no paid routes, no ceremonies).
3. A linter run over the snapshot is attached under `review/`.
4. `review/round-1/grok-4.7.md` and `review/round-1/fable.md` are written by those models from the
   same brief and snapshot, each without seeing the other's output.
5. `review/findings.md` is the deduplicated union, says which model found each item, marks the ones
   both found, and records that each was checked against the snapshot or source by the merger.
6. `disposition.md` accepts, rejects (with reason), or defers every finding; each accepted finding
   links a follow-on kanban task created through `ganda kanban reserve`/`claim`.
7. No product code changes on this task; fixes prefer generation or build checks over doc text.

## Checklist

- [x] Snapshot the published document into `review/` (`openapi-v1.snapshot.json`, `snapshot-meta.md`)
- [x] Write `review/review-brief.md` (lenses, severity scale, output format, probing rules)
- [x] Run linter, attach output (`review/lint/`: Redocly 2.83.0, Spectral 6.17.1)
- [x] Grok 4.7 review → `review/round-1/grok-4.7.md`
- [x] Fable review → `review/round-1/fable.md`
- [x] Merge + verify → `review/findings.md`
- [ ] Disposition with follow-on task links → `disposition.md` (written; 11 tasks linked; M-01's
      task waits on operator remediation, see `disposition.md` "M-01 (held)")

## Notes

- Skills: `tw-implementation-review` (review/ folder layout, merge → disposition on the same host
  task; no sibling apply-findings task), `tw-agent-collaboration`, `tw-web-api-contracts`.
- Precedent: `kanban/done/131-complete-repo-code-review-by-kimi-k3/`.
- Related: 257 (Microsoft.OpenApi 3.x), 331 (outside agent reviews feedback through the API).
- Output is kanban-only (review artifacts). Product changes belong to follow-on tasks.

## Session

- Created: f542f9e6-afba-4597-8f76-ca55f50824db (2026-10-31)
- Implement walk (Claude Opus 5.5, 2026-11-24): snapshot fetched 2026-11-24T20:15:44Z (OpenAPI
  3.2.0, 64 paths, 79 operations; the deployed host moved from 3.1.1 since creation). Grok 4.7
  (`grok -m grok-4.7`, headless) and Fable (`claude -p --model claude-fable-5-1`) ran in parallel
  from the same brief, each writing to a scratch file outside the repo, and were copied into
  `review/round-1/` after both ended. Merger verified every merged finding against the snapshot,
  source, or an unauthenticated live `GET`.
- Follow-on tasks reserved, claimed, specced, and published as children: 458-001 … 458-011.
- Held: Fable F-001 (merged M-01) is a live-host security finding. This repository is public, so
  its text, one sentence of Grok G-009, and its follow-on task are held outside the repo until the
  operator remediates. The walk parks Blocked on that.
- Audit: `kanban-landed` fails on task 315's kitchen. 315 is another live walk (claim alive, in its
  review step) whose kitchen has not landed yet; landing it from here would interfere with that
  walk, so it was left alone. This branch merged `origin/master` to clear its own drift.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 07a96c22-b168-4636-8e11-0e512ab5c2ea, max-turns 500, Blocked — 2026-11-24T20:57:40Z

## Results

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | pass (exit 0, 0 warnings, 0 errors; client-name scan passed) |
| Test files | none | no test file added or edited; no production code changed (kanban-only) |
| Audit | `ganda repo audit` | first run: 1 failure `kanban-landed` (315 kitchen drift and this branch behind master); merged `origin/master`; rerun: 1 failure `kanban-landed` for task 315 only (foreign live walk, not landed by this task); 9 pass, 1 skip |

Review outcome: 17 merged findings (1 blocker held, 9 major, 6 minor, 1 nit) plus one linter note;
16 accepted, 1 rejected (M-17). Both models agreed on M-02, M-03, M-04, M-05, M-06, M-13, M-14, M-15.

### How to validate

- **Smoke:** `python3 -c "import json,hashlib;b=open('review/openapi-v1.snapshot.json','rb').read();print(hashlib.sha256(b).hexdigest(),len(json.loads(b)['paths']))"`
  in this folder; then open `review/findings.md` and `disposition.md` and follow each 458-0NN link;
  `ganda kanban show 458-001` … `458-011`.
- **Expect:** hash `496a0506…9b98` and `64` paths (matches `review/snapshot-meta.md`); every
  disposition row except M-01 (held) and M-17 (rejected) links an existing to-do child task; both
  `review/round-1/grok-4.7.md` and `review/round-1/fable.md` exist and contain no secret values.

## Unblock update (2026-11-25)

Operator remediation is complete:

- The local dev Aspire host serving `/api/debug-config` on `arch.timewarp.work` has been stopped (verified `HTTP 502`; no longer serving `/api/debug-config`).
- All exposed rotatable credentials have been rotated (`OTEL_EXPORTER_OTLP_HEADERS` was Aspire's ephemeral dashboard OTLP key and `ASPIRE_USER_SECRETS_ID` is the public csproj GUID).
- The structural code fix **already landed on `master`**: **Task 524** (PR #690, `kanban/done/524-remove-api-debug-config-endpoint-from-common-server-module/task.md`) removed `/api/debug-config` from `CommonServerModule` and added a `404` regression test.
- Task 315 (PR #689) also merged to `master`, clearing the earlier `kanban-landed` drift.

Resumed walk actions:
1. Merge `origin/master` into this branch.
2. Restore the held F-001 text in `review/round-1/fable.md`, the held sentence in `review/round-1/grok-4.7.md` (G-009), and M-01 in `review/findings.md` from `~/.local/state/timewarp/task-458-held/` (strictly zero secret values).
3. Update `disposition.md` and the checklist: mark M-01 accepted and resolved by [524](../../done/524-remove-api-debug-config-endpoint-from-common-server-module/task.md) (PR #690) — do **not** file a duplicate follow-on task. Because `arch.timewarp.work` is currently stopped (`HTTP 502`), verify the removal from Task 524's merged code and `404` regression test rather than expecting a live host response.
4. Run `./bin/dev build` and `ganda repo audit`, commit, and finish.
