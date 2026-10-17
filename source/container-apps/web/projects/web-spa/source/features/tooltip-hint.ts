// #region Purpose
// Keeps every fluent-tooltip a hint popover so showing one does not close an open menu.
// #endregion
//
// #region Design
// FluentUI's fluent-tooltip sets popover to auto and shows it from a timer (immediately on focus,
// 250ms on hover) without checking that the pointer is still there. An auto popover's showPopover
// light-dismisses every other auto popover, including the Ask edit-mode and settings menus, so a
// late Copy tooltip closes a menu the person just opened. Task 370's test reopened the menu; that
// hid this. Hint popovers are the ones tooltips are for: showing a hint leaves an auto menu open,
// and showing the menu dismisses the hint. The type is read inside showPopover, and every FluentUI
// tooltip show goes through that method, including showTooltip. FluentUI also replaces the method,
// after whenDefined, with an isConnected guard that calls the function it captured. This wrapper
// composes with that override in either order, because each wrapper calls the function it captured.
// #endregion

const Hint = "hint";

interface TooltipShowHost {
  showPopover: (this: HTMLElement) => void;
  tweHintWrapped?: boolean;
}

function wrapShowPopover(constructor: CustomElementConstructor): void {
  const prototype = constructor.prototype as TooltipShowHost;
  if (prototype.tweHintWrapped === true) {
    return;
  }

  const showPopover = prototype.showPopover;
  prototype.showPopover = function (this: HTMLElement): void {
    if (this.getAttribute("popover") !== Hint) {
      this.setAttribute("popover", Hint);
    }

    showPopover.call(this);
  };
  prototype.tweHintWrapped = true;
}

void customElements.whenDefined("fluent-tooltip").then(wrapShowPopover);
