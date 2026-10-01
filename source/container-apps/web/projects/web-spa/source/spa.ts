// #region Purpose
// Root namespace object exposed as window.Spa by the JS initializer (web.spa.lib.module.ts).
// #endregion
//
// #region Design
// The Counter page's JavaScript onclick reads Spa.Counter.DispatchIncrementCountAction off this
// global. Plain object (NOT a class) all the same: Blazor's string-identifier JS interop resolver
// requires every intermediate path segment to be typeof "object", so any C# interop caller of a
// "Spa.*" identifier only resolves if Spa and its features are objects.
// Task 200: passkey C# no longer calls Spa.WebAuthn.* — it import()s web-authn.js named exports.
// WebAuthn stays on this object so a loaded initializer still exposes the global; Login must
// not depend on that. The Counter JS dispatch still requires the host initializer list to include
// Web.Spa (web-server MSBuild gate).
// #endregion
import { Counter } from "./features/counter.js";
import { WebAuthn } from "./features/web-authn.js";

export const Spa = {
  Counter,
  WebAuthn,
  // Additional features can be added here.
};
