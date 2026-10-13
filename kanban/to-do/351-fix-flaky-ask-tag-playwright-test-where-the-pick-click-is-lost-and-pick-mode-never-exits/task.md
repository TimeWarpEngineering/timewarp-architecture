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

- [ ] Reproduce + root cause (product vs test) recorded
- [ ] Deterministic fix (no retries/sleeps)
- [ ] 50× loop green; sibling Tag tests checked
- [ ] Gates: `dev build` 0/0, Playwright suite, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit; flake seen on #487 CI)
