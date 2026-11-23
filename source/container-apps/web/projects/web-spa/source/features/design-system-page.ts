// #region Purpose
// Presentational helpers for the design-system page: content scroll reset, and the
// resolved color of each token chip.
// #endregion
//
// #region Design
// The shell scroller is Fluent's content layout item. This page does not own that
// element. scrollContentToTop sets scrollTop to 0, then again on two frames, because
// a navigation can restore the old offset after the render that asked for the reset.
// readTokenColors paints each custom property onto a probe. getPropertyValue does not
// resolve color-mix; getComputedStyle on the probe does. The probe is created here,
// not in markup. watchTheme reports body[data-theme] changes so the printed values
// follow the theme button. Imported on demand, not via window.Spa.
// #endregion

interface ThemeHost {
  invokeMethodAsync(methodName: string): Promise<void>;
}

let themeHost: ThemeHost | null = null;
let themeObserver: MutationObserver | null = null;

export function scrollContentToTop(): void {
  const scroller = document.querySelector('.fluent-layout-item[area="content"]');
  if (!(scroller instanceof HTMLElement)) {
    return;
  }

  const reset = (): void => {
    scroller.scrollTop = 0;
  };

  reset();
  requestAnimationFrame(() => {
    reset();
    requestAnimationFrame(reset);
  });
}

export function readTokenColors(names: string[]): string[] {
  const probe = document.createElement("div");
  probe.setAttribute("aria-hidden", "true");
  probe.style.position = "absolute";
  probe.style.left = "-9999px";
  probe.style.width = "1px";
  probe.style.height = "1px";
  document.body.appendChild(probe);
  const values: string[] = [];
  try {
    for (const name of names) {
      probe.style.backgroundColor = `var(--${name})`;
      values.push(getComputedStyle(probe).backgroundColor);
    }
  } finally {
    probe.remove();
  }

  return values;
}

export function watchTheme(host: ThemeHost): void {
  themeHost = host;
  themeObserver?.disconnect();
  themeObserver = new MutationObserver(() => {
    void themeHost?.invokeMethodAsync("OnThemeChanged");
  });
  themeObserver.observe(document.body, { attributes: true, attributeFilter: ["data-theme"] });
}

export function stopThemeWatch(): void {
  themeObserver?.disconnect();
  themeObserver = null;
  themeHost = null;
}
