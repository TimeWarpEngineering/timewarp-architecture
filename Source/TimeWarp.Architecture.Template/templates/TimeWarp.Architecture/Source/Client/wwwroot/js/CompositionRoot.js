import { BlazorDualMode } from "./BlazorDualMode.js";
import { Counter } from "./Counter.js";
export class CompositionRoot {
    constructor() {
        this.BlazorDualMode = new BlazorDualMode();
        this.Counter = new Counter(window.BlazorState);
    }
    Initialize() {
        this.BlazorDualMode.ConfigureBlazor();
    }
}
