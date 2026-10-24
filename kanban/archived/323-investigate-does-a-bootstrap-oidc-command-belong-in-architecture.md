# Investigate: does a bootstrap-oidc command belong in Architecture?

Investigation only — open question, handled one at a time. Steven is not convinced this belongs in Architecture. Not approved to build, not scheduled. Source: CrunchIt-to-Architecture review, Busara (Wisdom) 2026-10-12.

## Question

Should Architecture ship a bootstrap-oidc command that creates the GitHub-to-Azure OIDC federated login used by CI deploys?

## Busara's reasoning

GREEN at medium priority. `aspire deploy` assumes the GitHub-to-Azure OIDC federated login but never creates it; Architecture `cicd.md` lists it as a manual prerequisite. `azd pipeline config` would do it, but it needs an azd project, is beta, and breaks the aspire-deployment skill's no-extra-deploy-tool rule. It pays off only once a client deploys from CI. Running it needs Owner rights (for User Access Administrator), so the command would have to check for that first.

## Steven's skepticism

Steven knows this relates to GitHub and Azure but is not sure it belongs in Architecture at all. It may fit better as a per-solution script, as docs only, or as a ganda/tooling concern.

## What the investigation should answer

- Does it belong in Architecture: yes or no.
- Evidence: which existing or likely solutions would use it, what Architecture or Fluent UI v5 already provides, and the cost of carrying it in the template.
- If no: where it belongs instead (per-solution code, docs only, or nowhere).
- Specifically weigh the alternatives: Architecture command vs per-solution script vs docs in `cicd.md` vs a ganda/tooling command.

## Notes

- Review notes: /workspace/ideas/2026-10-12-crunchit-to-architecture-review.md (v2 section), on the Grok Bot box.
- Investigation item, not a build task. Do not run `ganda task work` on it; take it up only when Steven picks it.

## Learnings

