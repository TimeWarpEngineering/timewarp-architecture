// #region Purpose
// Reports whether the viewport is the shell's icon rail.
// #endregion
//
// #region Design
// The rail is 880px through 1279px, the same bounds as the shell styles. A title
// tooltip belongs on that rail only: at full width the label is already visible.
// watch returns a handle for one menu. matches is the current query. stop removes
// that handle's listener, so a second menu keeps its own. Imported on demand, not
// via window.Spa.
// #endregion

const RailQuery = "(max-width: 1279px) and (min-width: 880px)";

interface RailHost {
  invokeMethodAsync(methodName: string, rail: boolean): Promise<unknown>;
}

export interface RailHandle {
  matches(): boolean;
  stop(): void;
}

export function watch(host: RailHost): RailHandle {
  const query = window.matchMedia(RailQuery);
  const onChange = (): void => {
    host.invokeMethodAsync("SetRail", query.matches).catch(() => undefined);
  };
  query.addEventListener("change", onChange);
  let stopped = false;
  return {
    matches(): boolean {
      return query.matches;
    },
    stop(): void {
      if (stopped) {
        return;
      }

      stopped = true;
      query.removeEventListener("change", onChange);
    },
  };
}
