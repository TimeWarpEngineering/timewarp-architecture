# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** same as framework

## Summary

The change adds an `OpenApiDocumentPath` constant, a closing prompt paragraph that names the OpenAPI URL, bearer calls, `/llms.txt`, and `/auth.md`, a bash + openssl + curl requirement with Git Bash or WSL for Windows, and two helper changes: `tr -d '\r\n'` and a trailing newline before `openssl base64 -d -A`. The Playwright test reads the OpenAPI URL from the rendered prompt and GETs it. That meets the agreement requirement for the OpenAPI path. The goal restatement matches the task. There is no misread.

Re-verified here: `web-spa-integration-tests` passed 555/555 (`dotnet run -c Release`, `TIMEWARP_TEST_PORT_BASE=43000`). On Linux, the new `b64url_decode` line round-tripped `hello-challenge`, and `json_string` extracted a value from a CRLF, spaced JSON body. The Playwright run and `dev build` were not re-run. The implementer reported both.

Risk is low. The findings are one missing agreement check and two small text or test-precision points.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/agent-links-onboarding-playwright-tests.cs:92
- Description: The prompt now hand-types `{origin}/llms.txt` and `{origin}/auth.md`. The tests only check that the prompt text contains those strings. No test checks that the server serves them (no test in `tests/` GETs either path). AGENTS.md says: "When two things must agree, generate one from the other or add a build check." If someone renames or drops either wwwroot file, every pasted prompt points the agent at a 404, and nothing fails.
- Suggestion: In the same Playwright test that already GETs the OpenAPI URL, also GET `origin + "/llms.txt"` and `origin + "/auth.md"` through `page.APIRequest` and require HTTP 200. Read those URLs from the rendered prompt as the OpenAPI URL is read, or reuse the strings the test already asserts.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/agent-links/agent-link-onboarding-prompt.cs:114
- Description: "not PowerShell, where curl is an alias for Invoke-WebRequest" is only true of Windows PowerShell 5.1. PowerShell 7 removed the `curl` alias. The real reason is that the blocks are bash and do not parse in any PowerShell. The same wording appears in the Design region (line 27).
- Suggestion: Give the bash reason first and scope the alias to Windows PowerShell. For example: "On Windows, run them in Git Bash or WSL. They do not run in PowerShell, and in Windows PowerShell curl is an alias for Invoke-WebRequest." Keep the test assertions in step with the new wording.
- Status: open

### Issue 3 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/agent-links/agent-link-onboarding-prompt-tests.cs:113
- Description: `prompt.ShouldContain("printf '%s\\n'")` is already satisfied by lines that existed before this change (`printf '%s\n' "$key_id" > …`, `printf '%s\n' "$link_json"`). So it does not pin the `b64url_decode` change it was added for. Reverting the helper to `printf '%s'` would still pass.
- Suggestion: Assert the specific helper line, for example `printf '%s\n' "$data$pad" | tr '_-' '/+' | openssl base64 -d -A`.
- Status: open
