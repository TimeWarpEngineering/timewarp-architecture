# Agent link prompt points the agent at the OpenAPI document and works on Windows, macOS, and Linux

## Description

Task 428 added a copy-paste prompt to the Agent links page
(`source/container-apps/web/projects/web-spa/features/agent-links/agent-link-onboarding-prompt.cs`,
rendered by `AgentLinksPage.razor`). It walks a shell-capable agent through key registration,
token issuance, and the link request with `openssl` and `curl`. Two gaps:

1. **OpenAPI.** After linking, the agent has no pointer to the rest of the API. The web server
   always serves its OpenAPI document at `/openapi/v1.json` and the Scalar UI at `/scalar/v1`
   (`CommonServerModule.UseScalarApiReference`, called unconditionally in
   `web-server/program.cs`). `wwwroot/llms.txt` and `wwwroot/auth.md` already list both. The prompt
   should tell the agent where the OpenAPI document is (full URL from the page's server origin) and
   that it describes every endpoint, request, and response, so the agent can use the API beyond
   the link ceremony with the bearer it mints. Mention `/llms.txt` and `/auth.md` too if that is
   short.
2. **Operating systems.** The prompt is a bash script. Confirm whether it runs unchanged on
   Linux, macOS, and Windows, and fix what does not.

## Requirements

- **OpenAPI pointer.** Add a short paragraph to the prompt: the OpenAPI document URL, what it
  covers, and that calls need `Authorization: Bearer <token>` from a fresh token ceremony (with the
  scopes the operation needs). Do not hand-type the path in a second place without a check: the
  OpenAPI path is already hand-written in `llms.txt` and `auth.md`. Either share one constant the
  web-spa can see, or add a test that fetches the path the prompt names from the running web
  server (the existing web-server or Playwright test hosts) and asserts 200 with an OpenAPI
  document. Prefer the smaller of the two.
- **OS check.** Go through the script line by line against:
  - **Linux:** GNU coreutils, OpenSSL 3.
  - **macOS:** BSD `sed`, `tr`, `date`, and the system `openssl` (LibreSSL), plus Homebrew OpenSSL 3.
    Check especially `openssl genpkey ... ec_paramgen_curve`, `openssl pkey -outform DER`,
    `openssl base64 -A`, `sed -n "s/.../p"` with `[[:space:]]`, and `date -u +%Y%m%dT%H%M%SZ`.
  - **Windows:** an agent usually has PowerShell, Git Bash (bundles `openssl` and `curl`), or WSL.
    In PowerShell 5, `curl` is an alias for `Invoke-WebRequest`. Check `chmod`, `$HOME`, and paths
    under Git Bash.
  Record the result per OS in the Results section. You can run Linux here. For macOS and
  Windows, reason from documented tool behavior and say which conclusions are unverified.
- **Fix, keep one script.** Make the bash script portable where a portable form exists (for
  example, POSIX-only `sed`, no GNU-only flags). Add one or two sentences to the prompt saying it
  needs bash with `openssl` and `curl`. On Windows that means Git Bash or WSL, not PowerShell. Do
  **not** write a second PowerShell recipe. If the bash script cannot be made to work on one of
  the three systems at all, stop and report it in the task instead of adding a variant.
- Keep the 428 design: routes, JSON names, and prefixes still come from the contracts, and the
  existing prompt tests (`agent-link-onboarding-prompt-tests.cs`,
  `agent-links-onboarding-playwright-tests.cs`) still pass. Extend them to cover the OpenAPI
  paragraph.
- Update the Design region of `agent-link-onboarding-prompt.cs` for both changes.

## Out of scope

- Changing whether or where the web server publishes OpenAPI.
- A PowerShell recipe or a packed client.
- Any change outside this repository. If something here seems to need one, stop and report.

## Checklist

- [ ] Prompt names the OpenAPI document URL and how to authenticate calls
- [ ] OpenAPI path agreement: shared constant or a test against the served document
- [ ] Per-OS result recorded (Linux, macOS, Windows), with unverified points marked
- [ ] Script portability fixes; prompt states the bash + openssl + curl requirement
- [ ] Prompt tests extended; `dev build` 0/0; `dev test` passes
- [ ] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)

## Notes

- Follow-up to task 428 (PR #590). Steve asked for both items on 2026-10-28.
