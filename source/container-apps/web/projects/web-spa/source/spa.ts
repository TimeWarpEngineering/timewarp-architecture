// #region Purpose
// Root namespace object exposed as window.Spa by the JS initializer (web.spa.lib.module.ts).
// #endregion
//
// #region Design
// Plain object (NOT a class): Blazor's string-identifier JS interop resolver requires every
// intermediate path segment to be typeof "object", so "Spa.Counter.DispatchIncrementCountAction"
// only resolves if Spa/Counter are objects.
// Task 200: passkey C# no longer calls Spa.WebAuthn.* — it import()s web-authn.js named exports.
// WebAuthn stays on this object so a loaded initializer still exposes the global; Login must
// not depend on that. Counter JS interop still requires the host initializer list to include
// Web.Spa (web-server MSBuild gate).
// #endregion
import { Counter } from "./features/counter.js";
import { WebAuthn } from "./features/web-authn.js";

export const Spa = {
  Counter,
  WebAuthn,
  // Additional features can be added here.
};
