import { BlazorDualMode } from "./BlazorDualMode.js";
import { Counter } from "./Counter.js";

export class CompositionRoot {
  public BlazorDualMode: BlazorDualMode = new BlazorDualMode();

  public Counter: Counter = new Counter(window.BlazorState);

  Initialize() {
    this.BlazorDualMode.ConfigureBlazor();
  }
}
