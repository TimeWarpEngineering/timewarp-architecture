# Fix flaky Ask Tag Playwright test where the pick click is lost and pick mode never exits

## Observed (2026-10-14, PR #487 `feature/ask-panel` → master)

The same commit (`7c77e74b3`) passed CI once (run 38432368932) and then failed once (run 38446310125,
a re-trigger from editing the PR) in `web-spa-playwright-tests`:

```
failed SignIn_Should_ReprobeAsk_And_NeverShowA401AsNotConfigured (46s)
  System.TimeoutException: Timeout 30000ms exceeded.
  - waiting for Locator("[data-qa=AskTagPicking]") to be hidden
    64 × locator resolved to visible <div role="status" data-qa="AskTagPicking" …>
  at AskSignIn_Given_Wasm.AssertTagPicksTheFeedbackRowAsync(IPage page)
```

After the test clicked the feedback row in pick mode, **pick mode never ended**. The click was not
taken as a pick. Task 346 (#483) added this flow.

## Determine first: test race or product race?

- **Product race:** clicking right after entering pick mode can be lost. Possible causes: the
  pointer/click listener from the `[SideEffectService]` pick module isn't armed yet; the overlay
  intercepts the click; or the State action that enters pick mode resolves after the click. A fast
  real user could hit this, so fix it in the product. Pick mode must be ready (listener armed, an
  observable state such as `data-picking-ready`) before the UI says "picking", or the first click
  must be queued.
- **Test-only race:** the test clicks before the ready signal. Then wait on a deterministic ready
  signal, not a timeout or retry.

Never "fix" this with retries, longer timeouts or `Thread.Sleep`-style waits.

## Requirements

- Reproduce reliably: loop the test (for example 30 to 50 runs) or add CPU throttling, and record
  the failure rate before and after.
- Fix at the root (product or test, per the above), with a deterministic readiness signal.
- The test passes 50/50 in a loop locally after the fix.
- Check the sibling Tag test paths (picking a notification bar, a card) for the same race.

## Checklist

- [x] Reproduce + root cause (product vs test) recorded
- [x] Deterministic fix (no retries/sleeps)
- [x] 50× loop green; sibling Tag tests checked
- [x] Gates: `dev build` 0/0, Playwright suite, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit; flake seen on #487 CI)
- 2026-10-14 implement (ganda task work): reproduced, fixed in product, 50/50 loop green, gates green.

## Results

### Root cause: product race (two of them)

1. **"Picking" before the pick was armed.** `StartPick` set `IsPicking` and re-rendered the status line
   *before* it imported `ask-tag.js` and called `Pick`. The status line was visible while no listener
   was live, so a key, hover or click right after it could be lost. A fast person could hit this too.
   The test had already worked around it once by waiting on the overlay before Esc.
2. **Orphan picks / cancel order.** `CancelPick` bumped the generation and then called the JS
   `CancelPick()` with no argument. When that call reached the browser before the pick's `Pick` call,
   the pick armed *after* the cancel. That left an armed pick that C# treated as stale, so its click was
   dropped. The next pick's status line could then stay on. Also, `CancelPick` cleared `IsPicking`
   before the browser had left pick mode, so the status line hid while the overlay was still up.

**Reproduction.** Unmodified, the test passed 1/1 locally and 6/6 under 4× CDP CPU throttling, so the
local rate was 0/7, against 1 failure in 2 CI runs. Injecting a 400 ms delay before `Pick` arms (the
window CI hits under load) made it fail 2/2. It timed out waiting for the hover outline after "picking"
showed. With the first fix and the same 400 ms delay in `Start`, it passed 2/3. The one failure exposed race 2's
cancel ordering (`outline` count 1 after the Cancel hid the line). After that fix it passed 6/6.

### Fix (no retries, no sleeps, no longer timeouts)

- `ask-tag.ts`: a pick is named by a token. `Start(root, token)` arms synchronously (overlay and all
  capture listeners live when it returns). `Answer(token)` resolves the pick's JSON, or `""`, and keeps
  a pick chosen between `Start` and `Answer`. `CancelPick(token)` ends only that pick, and a
  `Start` whose token was already cancelled is refused, so a cancel that overtakes its start never arms.
- `AskTagJsModule.PickAsync(js, token, armed, ct)`: Start, then ask the handler whether the pick is
  still current. A stale pick is cancelled right there; otherwise it waits for Answer.
- `AskTagState`: `PickGeneration` became `PickToken` (a Guid per pick, safe across circuits). `IsPicking`
  turns on only in the `armed` callback, so the status line is now the deterministic readiness signal.
  `IsPickPending` covers arming plus armed. Tag is disabled on it, and panel dispose cancels on it.
  `CancelPick` / `SessionChanged` clear the token first and `IsPicking` only after the browser has
  left pick mode.
- Playwright: removed the overlay-as-readiness workaround before Esc. The status line is the signal now.
- Integration tests: the fake module answers `Start`. New `Say_Picking_Only_Once_The_Browser_Has_Armed_The_Pick`
  and `Cancel_A_Pick_That_Is_Still_Arming` (cancel during arming, then a reordered `Start` arms: the
  stale pick disarms itself and never asks for an answer).

**Sibling Tag paths.** Every pick path in the test now waits on the armed status line: keyboard start,
Tab/Enter choose, panel Cancel, Esc, and hover+click. The only other Playwright file that touches Tag is
`ask-surface-playwright-tests.cs`, and it only measures layout boxes (no pick). No notification-bar or
card pick test exists yet. Those marks run through the same `Start`/`Answer` path.

**Boyscout:** `dev build` failed on master with new advisories GHSA-846f-f764-76mx / GHSA-2fmv-rv2c-59x7
(NU1902/NU1903 on `System.Security.Cryptography.Pkcs` 9.0.6, transitive via
`Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` 1.1.4 → NuGet.Packaging). Pinned 9.0.21 (first patched
9.x) in `Directory.Packages.props` and referenced it from the two analyzer test projects.

### Gates

- 50× loop of `SignIn_Should_ReprobeAsk_And_NeverShowA401AsNotConfigured`: **50/50 passed** (22 + 22 + 6).
- `dev build`: succeeded, 0 warnings / 0 errors.
- Playwright suite: 14/14. web-spa integration tests: 257/257 (AskTag 10/10). Analyzer tests 222/222,
  source generator tests 108/108.
- `ganda repo audit`: passes. `dev check-version`: source 2.0.0-beta.20 is ahead of NuGet (no bump needed).

### How to validate

**Smoke:**

```bash
dev build
cd tests/container-apps/web/web-spa-playwright-tests/bin/Debug/net11.0
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs:/etc/ssl/certs"
for i in $(seq 1 20); do ./web-spa-playwright-tests --filter-uid "AskSignInPlaywright_.AskSignIn_Given_Wasm.SignIn_Should_ReprobeAsk_And_NeverShowA401AsNotConfigured" | grep -E "succeeded:|failed:"; done
```

To stress the arming window, temporarily make `Start` in `ask-tag.ts` `async` with
`await new Promise(r => setTimeout(r, 400));` first. Rebuild and loop again, then revert.

**Expect:** every run reports `succeeded: 1` / `failed: 0`, including with the 400 ms arming delay.
Before the fix, that delay failed every run with a timeout waiting for the hover badge. By hand, in
`dev run`: on /Feedback, open Ask, press Tag, and click a row the moment the "Pick an element" line
appears. The chip is attached and pick mode ends.

