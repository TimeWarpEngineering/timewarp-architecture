# Round 1 — general
**Date:** 2026-11-02
**Scope reviewed:** the whole product diff of d6517bc46 against the merge-base with master (21 files)

## Summary

The change adds the token steps, configured lifetime, no-refresh rule, and reissue rule to the
`JWTBearerAuth` scheme through a web-host document transformer. Ceremony and list-feedback text
lives in contract `<remarks>`. To make those remarks reach generated endpoints on a csc build, a
new msbuild target passes the contract `.xml` as an AdditionalFile, and the FastEndpoint generator
reads it (`ContractDocumentation`). The dead `Produces(200, "Success")` chain is dropped. That
chain would have become live and broken the OneOf 200 schema once docs started flowing. The tests
match the acceptance list. The goal restatement matches the task, so there is no misread.

Re-verified in this worktree: `./bin/dev build` 0 warnings / 0 errors.
`OpenApiAgentReviewerGuide_` 3/3, `AdminFeedbackAuthorization_` 10/10, and source generator
tests 155/155. The main risk is shipping. The generator is a platform package, and the change
was not given a version bump. Generated apps would run the new tests against a generator that
cannot read the docs.

Checked and fine:
- Generated string escaping goes through the existing `EscapeForStringLiteral` path.
- The transformer does not append the guide twice when the scheme object is reused.
- List paging cannot hide the item: the default sort is `FiledAt` descending, and the test uses
  `MaxPageSize`.
- `msbuild/` already ships in the template, because `project-directory-metadata.props` is
  imported beside the new target.
- The tw-deploy line sits where the task asked.

## Issues

### Issue 1 — Severity: bug
- File: source/Directory.Build.props:14 (with Directory.Packages.props:38-40 and source/analyzers/timewarp-architecture-analyzers/models/contract-documentation.cs)
- Description: The change edits a platform package (`TimeWarp.Architecture.Generators`: the
  FastEndpoint generator now reads contract docs from an AdditionalFile). It does not bump
  `<Version>` (still `2.0.0-beta.30`) or the platform pins. The pin was moved to beta.30 in
  c13b430b5 ("take the 2026-10-30 nuget outdated pins"). Generated apps use
  `UseAnalyzerPackages=true` because `source/analyzers/**` is excluded from the template, so
  they restore the generator at that pin. That generator cannot read the contract `.xml`, so in
  a generated app:
  - the ceremony and list-feedback descriptions are missing from the OpenAPI document, which
    is the goal of this task;
  - the shipped tests `OpenApiAgentReviewerGuide_.Document_Given_.Issuance_Should_SayWhatTheClientSignsAndReturns`
    and `ListFeedback_Should_NameNewAndTheProcessedMarker` fail.

  The Results paragraph ("pick up contract remarks when that package includes this generator")
  admits the gap but leaves it open. AGENTS.md "Before a PR" requires `dev check-version` when
  packages ship, with the `<Version>` bump and the pins in the same commit. Commit c9a40020e is
  the precedent ("bump to 2.0.0-beta.28 so generated apps can call GetPrefix"). The Results do
  not record a `dev check-version` run. In this offline review sandbox it timed out, so release
  state could not be confirmed here.
- Suggestion: Run `dev check-version`. If beta.30 has already been published, bump `<Version>`
  in `source/Directory.Build.props` and the platform pins in `Directory.Packages.props` (and the
  timewarp-templates tree version, per check-version) to the next beta in one commit. Record the
  result in `## Results`. If beta.30 is not published, record that `dev check-version` passed
  and that the next release carries this generator.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/platform/identity-host/open-api-agent-reviewer-guide-server.cs:531
- Description: The guide names each step only by kebab id (`start-agent-key-registration`, …).
  No code in this repo sets OpenAPI `operationId` to those ids (grep for `OperationId` in
  foundation-server, the generator, and web-server finds none), and the ceremony summaries are
  prose ("Starts agent key registration…"). An agent holding only the document has to guess
  which path each id means. The document test checks only that the strings appear in order, not
  that each one resolves to an operation in `paths`. The task's bar is that an agent "given only
  `/openapi/{doc}.json` must be able to run the whole review loop."
- Suggestion: Put the method and path beside each id, for example
  `start-agent-key-registration (POST /api/identity/agent/register/options)`. Derive the path
  from the contract's `[ApiRoute]` so it cannot drift: the web host can reference
  `StartAgentKeyRegistration.Command` and its route provider. Extend
  `Scheme_Should_NameIssuanceStepsAndTheConfiguredLifetime` to assert that each named path
  exists under `paths` in the same document.
- Status: open
