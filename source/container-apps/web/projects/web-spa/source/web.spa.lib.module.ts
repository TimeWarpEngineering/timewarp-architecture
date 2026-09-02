// #region Purpose
// Blazor JS initializer: assign window.Spa before Web / WebAssembly / Server start.
// #endregion
//
// #region Design
// SDK discovers this file as **/$(PackageId).lib.module.js (PackageId = Web.Spa) and the host
// (web-server) aggregates it into web-server.modules.json / Blazor-Web-Initializers.
// wwwroot/js is gitignored TypeScript emit; task 116 compiles TS before web-spa SWA discovery.
// Task 200: web-spa re-globs emit into Content in that same target so the first host build of a
// clean tree still tags this initializer; web-server fails the build if the host list omits it.
// Passkey C# does not use window.Spa (on-demand import of web-authn.js). Counter still does
// (Spa.Counter.*), so a missed initializer remains a failed build rather than a silent skip.
// https://learn.microsoft.com/aspnet/core/blazor/fundamentals/startup
// #endregion

import { Spa } from "./spa.js";
import { log, LogAction } from "/_content/TimeWarp.State/js/Logger.js";
import {
  TimeWarpStateName,
  InitializeJavaScriptInteropName,
} from "/_content/TimeWarp.State/js/Constants.js";
import "/_content/TimeWarp.State.Plus/js/downloadFile.js";

// At this point Blazor is not yet initialized; attach the items you want on window here.
function initializeEnvironment() {
  console.log("****initializeEnvironment Web.Spa ****");
  // Spa is a plain object. Blazor's string-identifier interop resolver requires every
  // intermediate path segment to be `typeof === "object"` (see blazor.web.js), so the
  // namespace exposed on window must be plain objects, not classes.
  window.Spa = Spa;
}

export function beforeWebStart(_options: unknown, _extensions: unknown) {
  log("Web.Spa Web", TimeWarpStateName, "info", LogAction.Begin);
  log("Web.Spa Web", InitializeJavaScriptInteropName, "info", LogAction.Begin);
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
