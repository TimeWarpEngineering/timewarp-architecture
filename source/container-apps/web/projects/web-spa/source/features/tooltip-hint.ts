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
// and showing the menu dismisses the hint. The web component assigns popover in connectedCallback
// and FluentUI later wraps showPopover, so this sets the attribute when the node appears and again
// on that wrapper, after the library's own wrap.
// #endregion

const Hint = "hint";

interface TooltipShowHost {
  showPopover: (this: HTMLElement) => void;
  tweHintWrapped?: boolean;
}

function markHint(element: Element): void {
  if (element.localName === "fluent-tooltip" && element.getAttribute("popover") !== Hint) {
    element.setAttribute("popover", Hint);
  }

  for (const tooltip of element.querySelectorAll("fluent-tooltip")) {
    if (tooltip.getAttribute("popover") !== Hint) {
      tooltip.setAttribute("popover", Hint);
    }
  }
}

function watchTooltips(root: Element): void {
  const observer = new MutationObserver((records) => {
    for (const record of records) {
      if (record.type === "attributes") {
        if (record.target instanceof Element) {
          markHint(record.target);
        }

        continue;
      }

      for (const node of record.addedNodes) {
        if (node instanceof Element) {
          markHint(node);
        }
      }
    }
  });
  observer.observe(root, {
    attributes: true,
    attributeFilter: ["popover"],
    childList: true,
    subtree: true,
  });
  for (const tooltip of root.querySelectorAll("fluent-tooltip")) {
    markHint(tooltip);
  }
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

function wrapWhenDefined(): void {
  const defined = customElements.get("fluent-tooltip");
  if (defined !== undefined) {
    wrapShowPopover(defined);
  }
}

if (document.documentElement !== null) {
  watchTooltips(document.documentElement);
}

void customElements.whenDefined("fluent-tooltip").then(() => {
  wrapWhenDefined();
  queueMicrotask(wrapWhenDefined);
});
