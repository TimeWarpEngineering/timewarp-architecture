interface BlazorMethodReference {
  invokeMethodAsync: (methodName: string, ...args: any[]) => Promise<void>;
}

interface DisposeHandler {
  dispose: () => void;
}

export function NotifyLossOfInterest(
  elementId: string,
  blazorMethodReference: BlazorMethodReference
): DisposeHandler {
  const handleClick = (event: Event) => {
    const element = document.getElementById(elementId); // Re-acquire element
    if (element && !element.contains(event.target as Node)) {
      blazorMethodReference.invokeMethodAsync("NotifyLossOfInterest").then();
    }
  };

  const handleScroll = () => {
    blazorMethodReference.invokeMethodAsync("NotifyLossOfInterest").then();
  };

  document.addEventListener("click", handleClick);
  document.addEventListener("scroll", handleScroll);

  const observer = new MutationObserver((mutationsList) => {
    for (const mutation of mutationsList) {
      console.log(`The ${mutation.attributeName} attribute was modified.`);
      // debugger;
      if (
        mutation.type === "attributes" &&
        mutation.attributeName === "class"
      ) {
        const element = mutation.target as HTMLElement;
        console.log(`The ${mutation.attributeName} attribute was modified.`);
        if (window.getComputedStyle(element).opacity === "0") {
          console.log(`removing event listeners and disconnecting observer`);
          document.removeEventListener("click", handleClick);
          document.removeEventListener("scroll", handleScroll);
          observer.disconnect();
          break;
        }
      }
    }
  });

  const element = document.getElementById(elementId); // Acquire element
  if (element) {
    observer.observe(element, { attributes: true });
  }

  return {
    dispose: () => {
      document.removeEventListener("click", handleClick);
      document.removeEventListener("scroll", handleScroll);
      observer.disconnect();
    },
  };
}

console.log("NotifyLossOfInterestAsync.ts loaded");
