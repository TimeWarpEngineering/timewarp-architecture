// noinspection JSUnusedLocalSymbols,JSUnusedGlobalSymbols
import { Spa } from "./Spa.js";
import { Counter } from "./features/Counter.js";
import { Transition } from "./features/Transition.js";
import { NotifyLossOfInterest } from "./NotifyLossOfInterestAsync.js";
import { log, LogAction } from "/_content/Blazor-State/js/Logger.js";

// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-7.0&source=docs
// at this point the blazor is not yet initialized
// initialize the items you want attached to window here.

function initializeEnvironment() {
  console.log("****initializeEnvironment Web.Spa ****");
  Spa.Counter = Counter;
  Spa.Transition = Transition;
  window.Spa = Spa;
  window.NotifyLossOfInterest = NotifyLossOfInterest;
}
export function beforeWebStart(_options: any, _extensions: any) {
  log("TimeWarp.State Web", "beforeWebStart", "info", LogAction.Begin);
  initializeEnvironment();
}

export function afterWebStarted(_blazor: any) {
  log("TimeWarp.State Web", "afterWebStarted", "info", LogAction.End);
}

export function beforeWebAssemblyStart(_options: any, _extensions: any) {
  log(
    "TimeWarp.State WebAssembly",
    "beforeWebAssemblyStart",
    "info",
    LogAction.Begin
  );
  initializeEnvironment();
}

export function afterWebAssemblyStarted(_blazor: any) {
  log(
    "TimeWarp.State WebAssembly",
    "afterWebAssemblyStarted",
    "info",
    LogAction.End
  );
}

export function beforeServerStart(_options: any, _extensions: any) {
  log("TimeWarp.State Server", "beforeServerStart", "info", LogAction.Begin);
  initializeEnvironment();
}

export function afterServerStarted(_blazor: any) {
  log("TimeWarp.State Server", "afterServerStarted", "info", LogAction.End);
}
