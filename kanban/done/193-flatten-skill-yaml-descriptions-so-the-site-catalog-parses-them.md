# Flatten skill YAML descriptions so the site catalog parses them

## Description

timewarp.software QA: six architecture skills published with raw `>-` / `>` as
the description. The live site generator (software `master`) reads only the
first line after `description:`. Folded YAML scalars therefore become a
blockquote/empty callout on the catalog and skill pages.

## Requirements

- Single-line `description` (and `when-to-use`) on the six broken skills
- No `description: >` / `>-` left under `skills/**/SKILL.md`

## Checklist

- [x] Flatten tw-aggregate-pattern, tw-blazor, tw-feature-placement,
      tw-mock-response-factory, tw-slice-isolation, tw-web-api-contracts
- [x] Commit

## Results

All six skill frontmatters are single-line. A naive first-line parser will
publish the real description. Software `master` still needs the block-scalar
parser (separate PR) so future folded YAML stays safe.

### How to validate

```bash
rg -n '^description: *>' skills --glob 'SKILL.md'
# Expect: no matches

rg -n '^when-to-use: *>' skills --glob 'SKILL.md'
# Expect: no matches
```

After merge + site rebuild: https://timewarp.software/skills/index.json
descriptions for those six names are sentences, not `>-` / `>`.

## Session

- Implementation: grok 2026-08-13
