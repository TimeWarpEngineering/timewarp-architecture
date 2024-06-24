// noinspection JSUnusedLocalSymbols,JSUnusedGlobalSymbols
import { Spa } from "./Spa.js";
import { Counter } from "./features/Counter.js";
import { log, LogAction } from "/_content/TimeWarp.State/js/Logger.js";

// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-7.0&source=docs
// at this point the blazor is not yet initialized
// initialize the items you want attached to window here.
 
function initializeEnvironment() {
  console.log("****initializeEnvironment Web.Spa ****");
  Spa.Counter = Counter;
  window.Spa = Spa;
} 

export function beforeWebStart(_options: unknown, _extensions: unknown) {
  log("Web.Spa Web", "beforeWebStart", "info", LogAction.Begin);
  initializeEnvironment();
}

export function afterWebStarted(_blazor: unknown) {
  log("Web.Spa Web", "afterWebStarted", "info", LogAction.End);
}

export function beforeWebAssemblyStart(_options: unknown, _extensions: unknown) {
  log("Web.Spa WebAssembly", "beforeWebAssemblyStart", "info", LogAction.Begin);
  initializeEnvironment();
}

export function afterWebAssemblyStarted(_blazor: unknown) {
  log("Web.Spa WebAssembly", "afterWebAssemblyStarted", "info", LogAction.End);
}

export function beforeServerStart(_options: unknown, _extensions: unknown) {
  log("Web.Spa Server", "beforeServerStart", "info", LogAction.Begin);
  initializeEnvironment();
}

export function afterServerStarted(_blazor: unknown) {
  log("Web.Spa Server", "afterServerStarted", "info", LogAction.End);
}
