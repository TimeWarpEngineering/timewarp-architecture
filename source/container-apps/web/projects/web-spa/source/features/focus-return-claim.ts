// #region Purpose
// The document event an opening surface raises to take over another surface's pending focus return.
// #endregion
//
// #region Design
// Task 381: running the Ctrl-K Ask row closes the palette and opens Ask in the same flow, and the
// two OnAfterRender calls can land in either order. The palette keeps the element that had focus
// when it opened, to hand focus back on close. Ask needs that same element as its own opener, and
// its composer focus must not be undone by the palette's later return. Ask dispatches this event on
// every open; a palette still holding a return target writes it into the detail when Ask found
// nothing focused, and forgets it either way, so it never restores. The palette also hands over
// restore, which focuses the target behind its own guard: the target can be the palette's trigger
// (the appbar search field), whose focus would otherwise reopen the palette when Ask closes.
// One module so the event name has one definition.
// #endregion

export const FocusReturnClaimEvent = "twe-focus-return-claim";

export interface FocusReturnClaim {
  target: HTMLElement | null;
  /** Focuses target without side effects from its owner (the palette's trigger guard); null to focus directly. */
  restore: (() => void) | null;
}
