//interface BlazorMethodReference {
//  invokeMethodAsync: (methodName: string, ...args: any[]) => Promise<void>;
//}

//interface DisposeHandler {
//  dispose: () => void;
//}

//window.NotifyLossOfInterest = function (elementId: string, blazorMethodReference: BlazorMethodReference): DisposeHandler {
//  const element = document.getElementById(elementId);

//  let handleClick: (event: Event) => void;
//  let handleScroll: () => void;

//  handleClick = (event: Event) => {
//    if (element && !element.contains(event.target as Node)) {
//      blazorMethodReference.invokeMethodAsync('MethodName');
//    }
//  };

//  handleScroll = () => {
//    blazorMethodReference.invokeMethodAsync('MethodName');
//  };

//  document.addEventListener('click', handleClick);
//  document.addEventListener('scroll', handleScroll);

//  const observer = new MutationObserver(() => {
//    if (!document.contains(element)) {
//      document.removeEventListener('click', handleClick);
//      document.removeEventListener('scroll', handleScroll);
//      observer.disconnect();
//    }
//  });

//  observer.observe(document.body, {
//    childList: true,
//    subtree: true
//  });

//  return {
//    dispose: () => {
//      document.removeEventListener('click', handleClick);
//      document.removeEventListener('scroll', handleScroll);
//      observer.disconnect();
//    }
//  };
//};
