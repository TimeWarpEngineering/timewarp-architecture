window.NotifyLossOfInterest = function (elementId, blazorMethodReference) {
  const element = document.getElementById(elementId);

  function handleClick(event) {
    if (!element.contains(event.target)) {
      blazorMethodReference.invokeMethodAsync('NotifyLossOfInterestAsync');
    }
  }

  function handleScroll() {
    blazorMethodReference.invokeMethodAsync('NotifyLossOfInterestAsync');
  }

  document.addEventListener('click', handleClick);
  document.addEventListener('scroll', handleScroll);

  const observer = new MutationObserver(() => {
    if (!document.contains(element)) {
      document.removeEventListener('click', handleClick);
      document.removeEventListener('scroll', handleScroll);
      observer.disconnect();
    }
  });

  observer.observe(document.body, {
    childList: true,
    subtree: true
  });

  return {
    dispose: function () {
      document.removeEventListener('click', handleClick);
      document.removeEventListener('scroll', handleScroll);
      observer.disconnect();
    }
  };
};
