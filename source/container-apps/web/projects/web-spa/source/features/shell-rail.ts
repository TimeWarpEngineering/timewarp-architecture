// #region Purpose
// Reports whether the viewport is the shell's icon rail.
// #endregion
//
// #region Design
// The rail is 880px through 1279px, the same bounds as the shell styles. A title
// tooltip belongs on that rail only: at full width the label is already visible.
// watch returns the current match and calls SetRail when the match changes.
// stop removes the listener. Imported on demand, not via window.Spa.
// #endregion

const RailQuery = "(max-width: 1279px) and (min-width: 880px)";

interface RailHost {
  invokeMethodAsync(methodName: string, rail: boolean): Promise<unknown>;
}

let query: MediaQueryList | undefined;
let onChange: (() => void) | undefined;

export function watch(host: RailHost): boolean {
  stop();
  query = window.matchMedia(RailQuery);
  onChange = (): void => {
    const current = query;
    if (current === undefined) {
      return;
    }

    host.invokeMethodAsync("SetRail", current.matches).catch(() => undefined);
  };
  query.addEventListener("change", onChange);
  return query.matches;
}

export function stop(): void {
  if (query !== undefined && onChange !== undefined) {
    query.removeEventListener("change", onChange);
  }

  query = undefined;
  onChange = undefined;
}
