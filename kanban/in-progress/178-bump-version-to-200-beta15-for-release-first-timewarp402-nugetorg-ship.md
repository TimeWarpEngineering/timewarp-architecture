# Bump version to 2.0.0-beta.15 for release (first TimeWarp.402 nuget.org ship)

## Description

`2.0.0-beta.14` is already tagged and partially published (10/11 platform packages; **TimeWarp.402
did not exist** at the tag commit). Master has moved on (PR #298 + earlier work). Per `/tw-release`,
do **not** re-tag beta.14 — bump and cut a new release.

This bump is the release PR for shipping current master, including the first nuget.org publish of
`TimeWarp.402`.

## Checklist

- [x] Bump `<Version>` in `source/Directory.Build.props` and `timewarp-templates/Directory.Build.props`
- [x] Bump all platform CPM pins in `Directory.Packages.props` to the same version (task 124)
- [ ] `dev check-version` reports source ahead of latest published/tag
- [ ] PR green; merge to master
- [ ] Master CI green; cut with `dev release` from clean synced master

## Session

- Created after PR #298 merge: beta.14 tag at `a0f092d4`; master at `09c4f7d8`; check-version
  warned partial publish Missing TimeWarp.402 (new package, not a failed beta.14 push).
