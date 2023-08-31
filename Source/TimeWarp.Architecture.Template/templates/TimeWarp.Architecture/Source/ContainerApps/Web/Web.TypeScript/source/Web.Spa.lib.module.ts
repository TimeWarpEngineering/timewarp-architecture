import { Spa } from "./Spa.js";
import { Counter } from './features/Counter.js';

// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-7.0&source=docs
// at this point the blazor is not yet initialized
// initialize the items you want attached to window here.
export function beforeStart(options: any, extensions: any) {
  console.log("****beforeStart Web.Spa ****");
  Spa.Counter = Counter;
  window.Spa = Spa;
}

// at this point the blazor is initialized
export function afterStarted(blazor:any) {
  console.log("****afterStarted  Web.Spa ****");
}
