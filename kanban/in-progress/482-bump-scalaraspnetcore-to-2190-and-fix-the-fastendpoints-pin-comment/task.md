# Bump Scalar.AspNetCore to 2.19.0 and fix the FastEndpoints pin comment

## Description

Scalar.AspNetCore 2.19.0 reached nuget.org on 2026-11-05 after task 480's check. Bump the pin in
`Directory.Packages.props`. While there, correct the comment above the FastEndpoints pin: it says
FastEndpoints depends on Microsoft.AspNetCore.OpenApi, but that dependency belongs to
FastEndpoints.OpenApi (task 480 review nit M1). Either name the right package or drop the comment
if it no longer earns its lines.

## Requirements

- Scalar.AspNetCore 2.18.7 -> 2.19.0. Read the 2.19.0 notes; the API reference page is wired in
  web-server, so load it in the web-spa integration or Playwright suite if one covers it.
- Fix or remove the FastEndpoints comment. No other pin changes.
- `dev build`, `dev test`, `dev template-smoke`, `dev check-version` handled as in task 480.

## Checklist

- [ ] Scalar pin bumped
- [ ] FastEndpoints comment fixed or removed
- [ ] Gates green; check-version handled

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)

## Notes

Steve 2026-11-05: "bump it and fix/remove the FastEndpoints comment while at it".
