// #region Purpose
// Reports whether the shell nav column is the icon rail.
// #endregion
//
// #region Design
// The default rail is 880px through 1279px, the same bounds as tokens.css. A stored
// data-twe-nav="rail" is also the rail at 880px and wider, and "full" is never the rail.
// A title tooltip belongs on that rail only: at full width the label is already visible.
// watch returns a handle for one menu. matches is the current column, not only the media
// query. stop removes that handle's listeners, so a second menu keeps its own. Imported
// on demand, not via window.Spa.
// #endregion

const RailQuery = "(max-width: 1279px) and (min-width: 880px)";
const DesktopQuery = "(min-width: 880px)";

interface RailHost {
  invokeMethodAsync(methodName: string, rail: boolean): Promise<unknown>;
}

export interface RailHandle {
  matches(): boolean;
  stop(): void;
}

function isRail(): boolean {
  const choice = document.documentElement.getAttribute("data-twe-nav");
  if (choice === "rail") {
    return window.matchMedia(DesktopQuery).matches;
  }

  if (choice === "full") {
    return false;
  }

  return window.matchMedia(RailQuery).matches;
}

export function watch(host: RailHost): RailHandle {
  const query = window.matchMedia(RailQuery);
  const desktop = window.matchMedia(DesktopQuery);
  const onChange = (): void => {
    host.invokeMethodAsync("SetRail", isRail()).catch(() => undefined);
  };
  query.addEventListener("change", onChange);
  desktop.addEventListener("change", onChange);
  const observer = new MutationObserver(onChange);
  observer.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ["data-twe-nav"],
  });
  let stopped = false;
  return {
    matches(): boolean {
      return isRail();
    },
    stop(): void {
      if (stopped) {
        return;
      }

      stopped = true;
      query.removeEventListener("change", onChange);
      desktop.removeEventListener("change", onChange);
      observer.disconnect();
    },
  };
}
