export function addTransitionEndListener(
  element: HTMLElement,
  dotNetObject: any,
  methodName: string
): void {
  element.addEventListener("transitionend", function listener() {
    dotNetObject.invokeMethodAsync(methodName);
    element.removeEventListener("transitionend", listener);
  });
}
