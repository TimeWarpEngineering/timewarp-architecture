# Agent link prompt points the agent at the OpenAPI document and works on Windows, macOS, and Linux

## Goal (implementer restatement)

The Agent links paste prompt should keep the task 428 bash ceremony and also tell the agent where the rest of the API lives. After the link steps, the prompt names this server's OpenAPI document as a full URL (the page origin plus the document path), says that document describes every endpoint, request, and response, and says later calls send `Authorization: Bearer` plus a token from a fresh token ceremony with the scopes that operation needs. `/llms.txt` and `/auth.md` are named in the same short paragraph. The document path is written once. A test fetches the path the prompt actually names from the running web server and requires HTTP 200 plus an OpenAPI document.

The same prompt stays one bash script. It says the script needs bash, openssl, and curl, and that Windows must use Git Bash or WSL. The blocks are bash and do not run in PowerShell. The curl alias remark is limited to Windows PowerShell. The script uses forms that GNU, BSD, and Git Bash can run (POSIX `sed`, no GNU-only flags). Linux is executed here. macOS and Windows are judged from tool documentation, and anything not executed is marked unverified. There is no second PowerShell recipe and no client product name.

Round 1 left three open findings. This pass closes them: the Playwright test GETs `/llms.txt` and `/auth.md` from the URLs the rendered prompt names and requires HTTP 200; the prompt and its Design region give the bash reason and scope the alias to Windows PowerShell, and the tests match that wording; the helper assertion is the full `b64url_decode` line `printf '%s\n' "$data$pad" | tr '_-' '/+' | openssl base64 -d -A`.

Acceptance:

- The prompt names the OpenAPI URL and bearer authentication, and the prompt tests cover that paragraph.
- The OpenAPI path is not a second unchecked copy: one constant feeds the prompt, and the Playwright host fetches the URL the rendered prompt names.
- The Playwright test also GETs the `/llms.txt` and `/auth.md` URLs the prompt names and requires HTTP 200.
- The prompt says the blocks are bash and do not run in PowerShell, and it scopes the curl alias to Windows PowerShell. Matching tests use that wording.
- The `b64url_decode` test asserts the full helper line, not a `printf '%s\n'` fragment that other lines already contain.
- Results record Linux, macOS, and Windows, with unverified points marked.
- The script is portable where a portable form exists, and the prompt states the bash + openssl + curl requirement.
- `dev build` is 0 warnings and 0 errors, and the prompt tests pass.
- Source, tests, and skills do not name a client product.

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

- [x] Prompt names the OpenAPI document URL and how to authenticate calls
- [x] OpenAPI path agreement: shared constant or a test against the served document
- [x] Per-OS result recorded (Linux, macOS, Windows), with unverified points marked
- [x] Script portability fixes; prompt states the bash + openssl + curl requirement
- [x] Prompt tests extended; `dev build` 0/0; `dev test` passes
- [x] No client name in source, tests, or skills
- [x] Playwright GETs the `/llms.txt` and `/auth.md` URLs the prompt names (HTTP 200)
- [x] Prompt says the blocks are bash and do not run in PowerShell; the curl alias is Windows PowerShell only
- [x] `b64url_decode` assertion is the full helper line

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18374-969e-7912-83a2-d3297ead0cd3, max-turns 500, Blocked — 2026-10-28T17:00:44Z
- Implement oracle resumed by implementer-grok (grok, model grok-4.7) after the placeholder block (2026-10-29)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18376-4e20-79b0-9f58-e957007e6d70, max-turns 500, Done — 2026-10-28T17:36:07Z
- Review oracle round 1: claude-opus-5-5 (headless ganda task work), effort 2, roster general. Three findings are open (M1 suggestion, M2 and M3 nits), so a fix pass is needed. 2026-10-29
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session 62d82ad0-0853-4703-91f1-91110ba4cbb3, max-turns 120 — 2026-10-28T17:40:23Z
- Fix pass round 1 (implement-fix-2): M1, M2, and M3 implemented on this id (2026-10-29)

## Review

- Round 1: three findings (M1 suggestion, M2 nit, M3 nit). The fix pass implemented them on this id. Round 2 is the host review node.

## Notes

- Follow-up to task 428 (PR #590). Steve asked for both items on 2026-10-28.

## Results

The prompt ends with one paragraph. It names the page origin plus `AgentLinkOnboardingPrompt.OpenApiDocumentPath` (`/openapi/v1.json`), says that document describes every endpoint, request, and response, and says later calls send `Authorization: Bearer` plus a token from a fresh token ceremony with the scopes that operation needs. The same paragraph names `{origin}/llms.txt` and `{origin}/auth.md`. The path literal lives only on that constant. The spa integration test asserts the built prompt contains the origin plus the constant. The Playwright project does not reference web-spa, so its test reads the URL the rendered prompt names (`origin + "/openapi/"` through the next `.json`) and GETs it through the page API request. It requires status 200, a JSON `openapi` string, and a `paths` object whose raw text contains `" /`. The same test reads `{origin}/llms.txt` and `{origin}/auth.md` out of that prompt and GETs each one, requiring HTTP 200. OpenAPI publication is unchanged.

The opening sentence says the blocks need bash, openssl, and curl. On Windows they run in Git Bash or WSL. They do not run in PowerShell, and in Windows PowerShell `curl` is an alias for `Invoke-WebRequest`. The Agent links page caption says the blocks are bash and do not run in PowerShell. There is one script.

Portability changes in the shared helpers: `json_string` strips CR and LF before POSIX `sed` (`sed -n` with BRE groups and `[[:space:]]`, no GNU-only flags). `b64url_decode` writes a trailing newline before `openssl base64 -d -A`. `bash -n` of the helper text succeeded.

### Operating systems

**Linux (executed).** OpenSSL 3.5.5 (27 Jan 2026) and GNU bash 5.3.9. `date -u +%Y%m%dT%H%M%SZ` matched `^[0-9]{8}T[0-9]{6}Z$`. `tr -d '\r\n'` strips CR and LF. `openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256` then `openssl pkey -in agent.pem -pubout -outform DER` produced a 91-byte SPKI (`BEGIN PUBLIC KEY`, DER starts `30 59`). `openssl base64 -A` and `openssl base64 -d -A` round-tripped `hello-challenge` with and without a trailing newline. `openssl dgst -sha256 -sign` produced a 70-byte DER SEQUENCE of two INTEGERs. POSIX sed extracted a string from spaced JSON and from CRLF JSON. The greedy `.*` still matches the last key of that name. That behavior is pre-existing and was left as-is. `bash -n` of a representative block succeeded.

**macOS (unverified; not executed on macOS).** Judged from the OpenBSD openssl(1) manual, which documents `genpkey -pkeyopt ec_paramgen_curve:curve`, `enc -A` (base64 on one line), and `pkey` as converting public and private keys, plus `-outform der|pem` on key commands including genpkey. Apple `/usr/bin/openssl` is LibreSSL. Homebrew OpenSSL 3 matches the Linux OpenSSL 3 flags when that binary is first on PATH. BSD `date -u` accepts this POSIX strftime stamp. POSIX `sed -n` with `s///p` accepts `[[:space:]]` and BRE groups. POSIX `tr` accepts `\n` and `\r`. macOS `/bin/bash` 3.2 supports `pipefail`, `${#var}`, `${DIR:?}`, and functions. The script uses no bash 4 feature. `chmod` and `$HOME` are standard. The conclusion that `openssl pkey -pubout -outform DER` succeeds on Apple LibreSSL was not executed on macOS.

**Windows (unverified; not executed on Windows).** The blocks are bash, so they do not run in PowerShell. Windows PowerShell 5.1 aliases `curl` to `Invoke-WebRequest`, which rejects `curl -fsS`. PowerShell 7 has no such alias. The prompt tells the agent to use Git Bash or WSL and scopes the alias to Windows PowerShell. Git Bash ships bash, GNU-like sed, tr, and date, plus openssl and curl. Quoted `$HOME` paths stay in the MSYS profile (`/c/Users/...`). The script does not use drive letters. MSYS `chmod` typically exits 0 and does not apply Unix mode bits on NTFS. WSL is a Linux userspace equivalent to the Linux run and was not executed here.

### Build and tests

`./bin/dev build` finished with 0 warnings and 0 errors. The client-name scan passed (`crunchit` and `copic` are absent).

`./bin/dev test --no-build` ran twice on the first implement pass. Both times 21 of 23 projects passed. `web-spa-playwright-tests` passed 32/32 both times, and `web-spa-integration-tests` passed both times. The two failures were `web-jaribu-tests` and `web-server-integration-tests`, both assigned port base 21500. Each failed in `HostGraphFactory.EnsurePortIsFree` because port 21500 was already in use while another project still held that base. Those projects do not cover this prompt. Run alone on free bases, `web-server-integration-tests` with `TIMEWARP_TEST_PORT_BASE=42000` exited 0, and `web-jaribu-tests` with `TIMEWARP_TEST_PORT_BASE=40000` passed 441/441.

The fix pass rebuilt with `./bin/dev build` (0 warnings, 0 errors, client-name scan passed). `Build_Should` in `web-spa-integration-tests` passed 1/1 (`TIMEWARP_TEST_PORT_BASE=43100`, `--filter-class Build_Should`). `AgentLinksOnboarding_Given_Wasm` in `web-spa-playwright-tests` passed 1/1 (`TIMEWARP_TEST_PORT_BASE=43200`, `--filter-class AgentLinksOnboarding_Given_Wasm`), including the new `/llms.txt` and `/auth.md` GETs.

### Review

Round 1 had effort 2 and one general reviewer. There were 0 bugs, 1 suggestion, and 2 nits. The fix pass implements all three on this id. M1: the Playwright test reads `/llms.txt` and `/auth.md` from the rendered prompt and requires HTTP 200. M2: the prompt, Design region, page caption, and tests say the blocks are bash and do not run in PowerShell, and they scope the curl alias to Windows PowerShell. M3: the integration test asserts `printf '%s\n' "$data$pad" | tr '_-' '/+' | openssl base64 -d -A`. Round 2 disposition is still the host review node. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

### How to validate

Smoke: Sign in, open the Agent links page, and read the prompt. It names bash, openssl, and curl, says the blocks run in Git Bash or WSL on Windows and do not run in PowerShell, scopes the curl alias to Windows PowerShell, and ends with the origin plus `/openapi/v1.json`, bearer authentication, `/llms.txt`, and `/auth.md`.

Expect: `./bin/dev build` is 0 warnings and 0 errors. `Build_Should` passes, including the full `b64url_decode` line and the Windows PowerShell wording. The Playwright onboarding test passes, including a GET of the OpenAPI URL the rendered prompt names (HTTP 200, an `openapi` string, and a `paths` object) and GETs of the `/llms.txt` and `/auth.md` URLs that prompt names (HTTP 200).
