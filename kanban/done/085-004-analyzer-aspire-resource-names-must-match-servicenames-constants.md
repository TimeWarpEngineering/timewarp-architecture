# Analyzer: Aspire resource names must match ServiceNames constants

## Parent
085-analyzer-and-source-gen-opportunities-to-remove-inference-collected-candidates

## Description

Documented trap: AppHost resource names must equal the `ServiceNames` constants
(web-server/api-server/grpc-server) or server-side `BaseAddress` resolves null — and it only
bites under server render (Auto), making it a delayed runtime failure. Analyzer (app-host
compilation only): string literals passed as resource names to `AddProject(...)` must be members
of `ServiceNames`.

## Checklist

- [x] **TWA0007** in the convention-analyzers assembly. Scope gate refined: rather than an
      Aspire.Hosting reference check, the analyzer is silent unless the compilation can resolve
      `TimeWarp.Foundation.Configuration.ServiceNames` — and only `AddProject` first arguments
      are examined, so nothing else in the repo pays.
- [x] Names checked via **semantic constant evaluation** (not just literals) — literals, const
      references, and aliases of ServiceNames all resolve; non-constant names are skipped.
      `AddYarp`/`AddPostgres`/container resources deliberately out of scope.
- [x] Fixie tests (5): matching literal clean; ServiceNames-qualified clean; aliased local const
      clean; unknown name flagged at the argument; non-constant skipped; no-ServiceNames silent.
      Analyzer suite 31/31.
- [x] Reconcile went further than "already clean": **the inference was removed at its source** —
      app-host `Constants` were duplicating ServiceNames values under a "These MUST match"
      comment; they now alias `ServiceNames.*` directly (const-to-const), so drift is impossible,
      with TWA0007 guarding any hand-written name. Required adding a compile-time
      foundation-contracts reference to app-host (`IsAspireProjectResource="false"`, with the
      repo/package switch) — Aspire's project references are app-model resources, not compile refs.
- [x] End-to-end negative test in the REAL app-host: sabotaged one constant → TWA0007 fired with
      the allowed-values list in the message → reverted. The delayed null-BaseAddress runtime
      failure (memory: only bites under server-side rendering) is now a build error.
- [x] `dev build` 0/0; analyzers 31, sourcegen 16, contracts 7, web-server 22, api-server 6.

## Notes

- Trap documented in memory `aspire-resource-names-must-match-servicenames` and in
  aspire-app-host/constants.cs inline comment.
