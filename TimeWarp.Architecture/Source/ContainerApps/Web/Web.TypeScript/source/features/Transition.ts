import { Spa } from "../Spa.js";

export class Transition extends Spa {
  public static AddTransitionEndListener(
    element: HTMLElement,
    dotNetObject: any,
    methodName: string
  ) {
    console.log("%cAddTransitionEndListener", "color: green");
    // Add some label to the console logs below.
    console.log(element);
    console.log(dotNetObject);
    console.log(`methodName: ${methodName}`);
    element.addEventListener("transitionend", function listener() {
      console.log(
        "%cTransitionend fired. Attempting to call back to C#",
        "color: green"
      );
      dotNetObject.invokeMethodAsync(methodName);
      element.removeEventListener("transitionend", listener);
    });
  }
}
