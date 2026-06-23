// Spa.ts — root namespace object exposed as window.Spa by the JS initializer
// (web.spa.lib.module.ts). Plain object (NOT a class): Blazor's string-identifier
// JS interop resolver requires every intermediate path segment to be typeof "object",
// so "Spa.Counter.DispatchIncrementCountAction" only resolves if Spa/Counter are objects.
import { Counter } from "./features/counter.js";

export const Spa = {
  Counter,
  // Additional features can be added here.
};
