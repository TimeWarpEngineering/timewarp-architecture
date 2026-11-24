# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch `task/519-move-feature-filename-grammar-generator-into-dev` vs merge-base `f47562750` (17 files). Compared with `## Goal (implementer restatement)` and the original requirements.

## Summary

The C# generator in `tools/dev-cli/services/feature-filename-grammar-generator.cs` replaces `generate-feature-filename-grammar.py`. It keeps the same four outputs, the same function-to-routed-layer refusal, and the same nested-suffix refusal. `dev regenerate-generated` runs it and skips a missing JSON file. `dev workflow` fails the fast-group drift check, naming `dev regenerate-generated`, when `goldens/ingress` or any of the four grammar outputs changes. The MSBuild `<Exec>` target, the `_FeatureFilenameGrammar*` properties, and the Python script are gone. Live docs and skills name `dev regenerate-generated`. The restatement matches the task. Historical kanban cards still name the old script; those are past-task records, which the requirement's docs/skills/comments/tests list does not ask to rewrite.

The four committed outputs differ from the Python-era files only in the header line and the `.g.cs` Families doc comment that named the removed `<Exec>`. `## Results` records self-install, regenerate (header-only diff, no ingress diff), build 0/0, dev-cli-tests 9/9, the analyzer sync test 1/1, ingress helper tests 4/4, template-smoke success, and a clean audit after `--fix`. That record covers this change. No full build, test, or audit was re-run.

A single probe, `dotnet run --file /tmp/raw-nl-probe/probe.cs`, checked one suspected emitter bug: that the raw-string templates plus `Normalize`'s extra `\n` would add a blank line between props `ItemGroup`s and a second trailing newline. The probe showed the raw string does not keep the newline before the closing quotes, so `Normalize` adds the one trailing LF the committed files already have. Not a defect.

No issues.
