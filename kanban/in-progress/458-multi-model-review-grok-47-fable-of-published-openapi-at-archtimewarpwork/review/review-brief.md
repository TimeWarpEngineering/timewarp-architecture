# Review brief — published OpenAPI of arch.timewarp.work (task 458, round 1)

You are one of two independent reviewers. The other reviewer gets this same brief and snapshot.
You do **not** see their output, and they do not see yours.

## Target

- **Review target:** `review/openapi-v1.snapshot.json` in this task folder (see
  `review/snapshot-meta.md`: fetched 2026-11-24T20:15:44Z, OpenAPI `3.2.0`, 64 paths, 79
  operations, 175 schemas). This is what ships to consumers: humans, generated clients, and agents.
- You review the **published document**, not the source diff. You may read repo source to explain a
  finding or to name the owner of a fix:
  - contracts: `source/container-apps/web/web-contracts/` (and `api-contracts`, `grpc-contracts`)
  - the FastEndpoints generator and the OpenAPI wiring in `source/container-apps/web/web-server/`
    (search for `AddOpenApi`, `IOpenApiDocumentTransformer`, `IOpenApiOperationTransformer`,
    `x-timewarp-excluded-routes`)
  - analyzers and generators under `source/analyzers/`, `source/generators/` (or wherever `rg` finds them)
  - skill: `skills/tw-web-api-contracts/`
- Rendered view: https://arch.timewarp.work/scalar/v1 · index: https://arch.timewarp.work/llms.txt

## Probing rules (hard)

- Live probing is **unauthenticated `GET` only**: `/api/health`, `/health`, `/llms.txt`, the
  `/.well-known/*` discovery documents, the OpenAPI document itself, and free `GET` routes that
  the document says need no auth.
- **No** `POST`, `PUT`, `DELETE`. **No** paid routes: `/api/tip`, `/api`, `/api/demo/metered-capability`
  (not even `GET`). **No** identity ceremonies (`/api/identity/*` options/register/authenticate,
  Entra challenge). No credentials, no tokens.
- Do not write to the snapshot. Do not edit anything outside your one output file.

## Lenses

1. **Validity** — OpenAPI 3.2.0 / JSON Schema 2020-12 conformance. Linter output is attached under
   `review/lint/` (Redocly and Spectral). Read it; do not re-run the linters unless you need to.
   Note where a linter is wrong because it does not know 3.2.0.
2. **Consistency** — path casing and prefixes (`/Analytics/TrackEvent` vs `/api/...`,
   `/api/Feedback` vs `/api/admin/feedback`, `/api/Users/Current/...` vs `/api/identity/...`,
   `/identity/sign-out` outside `/api`), operationId and tag naming, parameter naming and casing.
3. **Contracts** — request and response schemas; whether `OneOf<Response, SharedProblemDetails>`
   appears as `application/problem+json` problem details with the right status codes; the 402/503
   rules stated in `info.description`; nullability; enums; examples; schema naming and duplication.
4. **Security** — security schemes, per-operation `security` requirements, admin vs user vs agent
   routes, anonymous routes that should not be, and anything the document leaks (internal names,
   hosts, secrets, stack details).
5. **Agent and generated-client consumability** — `summary`, `description`, `operationId`
   uniqueness and quality, the `[Operation]`-derived metadata, tags. Would Kiota or NSwag produce
   sane output (method names, model names, unions)? Running a generator is optional; reasoning
   from the document is fine.
6. **Doc vs reality** — spot-check that documented free `GET` routes behave as documented
   (status, content type, shape), and that the excluded-routes list (`info.description` and
   `x-timewarp-excluded-routes`) matches what is actually served.

## Severity scale

| Severity | Meaning |
|----------|---------|
| blocker | The document is invalid, unsafe, or misleads a consumer into a wrong call that matters (security, money, data loss). |
| major | A generated client or agent gets something materially wrong, or the doc contradicts the live behavior. |
| minor | Inconsistency or missing metadata that degrades use but does not mislead. |
| nit | Cosmetic. |

## Output format

Write **one** Markdown file at the path you were given. Start with a short header (model, date,
what you read, what you probed). Then one section per finding:

```markdown
### <ID> — <one-line title>

- **Severity:** blocker | major | minor | nit
- **Lens:** validity | consistency | contracts | security | consumability | doc-vs-reality
- **Pointer:** JSON pointer(s) into the snapshot, e.g. `/paths/~1api~1Feedback/post/responses/400`
- **Evidence:** quote the snapshot (short), linter line, probe result, or source file:line
- **Proposed fix:** what to change
- **Owner:** contract | generator | transformer | skill | host config | other (name the file if known)
```

Use IDs `G-NNN` (Grok) or `F-NNN` (Fable). Prefer fixes that **generate the document from
contracts or add a build check** over hand-patched description text. Do not pad: a short list of
real findings beats a long list of guesses. End with a "What looks right" section of a few bullets
so the merger knows what you checked and found clean.
