# Skill evaluations

[Vally](https://www.npmjs.com/package/@microsoft/vally-cli) evals for TimeWarp agent skills.
Pattern follows [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills) and
[dotnet-skills-evals](https://github.com/Aaronontheweb/dotnet-skills-evals).

## Install

```bash
npm install -g @microsoft/vally-cli
vally --version
```

## Layout

```
.vally.yaml                          # suite filters (ci-gate, nightly)
.github/workflows/skill-lint.yaml      # PR schema gate (no model token)
evals/
  contracts/fixtures/                # shared contract snippets for stimuli
  non-contracts/                     # negative-routing fixtures
skills/<skill>/evals/
  eval.yaml                          # stimuli + graders
```

## Quick commands

```bash
# Validate specs (no model token required)
vally lint skills
vally lint --eval-spec skills/web-api-contracts/evals/eval.yaml

# Run one skill's ci-gate stimuli
vally eval \
  --eval-spec skills/web-api-contracts/evals/eval.yaml \
  --skill-dir skills \
  --tag priority=p0,p1 \
  --output-dir ./results

# Routing stimuli only
vally eval \
  --eval-spec skills/web-api-contracts/evals/eval.yaml \
  --tag area=routing

# Plan without grading (smoke executor wiring)
vally eval \
  --eval-spec skills/web-api-contracts/evals/eval.yaml \
  --skip-grade
```

## Authentication

The default executor is `copilot-sdk`. Token resolution order:

1. `COPILOT_GITHUB_TOKEN` env var
2. `GH_TOKEN` / `GITHUB_TOKEN`
3. OAuth token from `copilot login` (stored in system keychain / `~/.copilot` on Linux)

**Local (recommended):** `copilot login` is enough — no PAT required if `copilot -p "hello"` works.
vally picks up the same credential automatically.

```bash
npm install -g @github/copilot
copilot login
copilot -p "say hello"   # sanity check
```

**CI / headless:** use the same OAuth token as local auth — run `copilot login` once on the
runner/bot account and persist `~/.copilot/config.json` (or keychain) as a CI secret. That is
what vally's `copilot-sdk` executor expects.

A fine-grained PAT with **Copilot Requests** (Account permission, personal resource owner only)
is documented for headless use, but many accounts do **not** show it in the PAT UI. If you only
see **Copilot agent settings**, that is a different permission (org coding-agent admin APIs) and
will **not** authenticate Copilot CLI / vally inference. Do not use it for evals.

**Model:** set `defaults.model` in `eval.yaml` to a model your Copilot plan exposes.
List yours: `copilot -p "list available models"`. Default in `eval.yaml` is `gpt-5-mini`;
override ad hoc: `--model claude-sonnet-4.6`.

Without any Copilot auth, use `vally lint` for schema checks only.

## Interpreting results

- `results/<run>/eval-results.md` — summary table
- `vally serve ./results` — local dashboard (http://127.0.0.1:3200)

## Adding evals

See `skills/web-api-contracts/evals/eval.yaml` as the reference. Authoring rules
(from aspire-skills):

- Phrase prompts like a real developer, not a spec
- `prompt` graders must say "the assistant's response"
- Split positive intent graders from `output-not-contains` anti-patterns
- Forbid full commands in `not_contains`, not bare nouns (`"azd up"` not `"azd"`)