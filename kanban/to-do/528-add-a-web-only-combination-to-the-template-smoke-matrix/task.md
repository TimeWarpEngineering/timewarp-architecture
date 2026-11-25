# Add a web-only combination to the template-smoke matrix

## Description

`dev template-smoke --matrix` generates the defaults plus each flag off on its own (NoPostgres, NoApi,
NoWeb, NoGrpc, NoYarp). No variant turns several flags off together. The combination the first
instance repo will be generated with (see task 491) is web only: `--api false --grpc false --yarp false`,
postgres on. Nobody builds that output today.

Add that one combination to the matrix so it is generated, built, tested and published every night.
Fix whatever it turns up.

## Propagation

template-only

## Requirements

- A `SmokeWebOnly` variant in `SmokeMatrix`: api, grpc and yarp off; web and postgres on.
- Its `ExcludedFamilies` list covers api, grpc and yarp, so an orphaned aggregator or test project
  from any of the three fails the smoke.
- The variant runs wherever the other matrix entries run: `dev template-smoke --matrix` and the
  nightly `template-smoke` job in `.github/workflows/workflow.yml`.
- `dev template-publish-smoke` (or the publish suites the matrix already runs) covers the variant for
  `aca` and `aca-neon`, the targets a web-only app deploys to.
- Any generation, build, test or publish failure in the variant is fixed in this task, or filed as
  its own task and named in Results when it is large.

## Checklist

- [ ] Add `SmokeWebOnly` to `tools/dev-cli/services/template-smoke/template-smoke-matrix.cs`.
- [ ] Update the header comment there ("defaults plus each flag off on its own").
- [ ] Nightly job picks the variant up (matrix list or name filter in the workflow, if it has one).
- [ ] `dev template-smoke --matrix` green for the new variant.
- [ ] Publish smoke green for the new variant on `aca` and `aca-neon`.
- [ ] Check off the multi-flag item on task 071 ("spot-check a couple multi-flag-off combos") or note
      there that this task covers one of them.

## Notes

- One combination, not the full power set. We are the consumer; this is the shape our first instance uses.
- Task 373 added the single-flag web, grpc and yarp variants. Task 395 fixed the NoYarp publish failures.
- Task 527 changes what the Azure publish output looks like with yarp off (web-server becomes the
  external app). Whichever of the two lands second updates the expectation. No hard dependency.

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)

## Results (required before done)

### How to validate (required before done)
