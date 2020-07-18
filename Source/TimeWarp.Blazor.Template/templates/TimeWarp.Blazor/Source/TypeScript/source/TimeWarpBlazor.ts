export class TimeWarpBlazor {
  private applicationVersion = 'TimeWarp.Blazor.0.0.1';
  private clientApplicationKey = "clientApplication";
  private executionSideKey = "executionSide";
  private clientApplicationValue = localStorage.getItem(this.clientApplicationKey);
  private executionSideValue = localStorage.getItem(this.executionSideKey);
  private clientLoaded = this.clientApplicationValue === this.applicationVersion;

  HelloWorld() {
    console.log("Hello World");
  }

  DispatchIncrementCountAction() {
    console.log("%cdispatchIncrementCountAction", "color: green");

    const IncrementCountActionName =
      "TimeWarp.Blazor.Features.Counters.CounterState+IncrementCounterAction, TimeWarp.Blazor.Client, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    BlazorState.DispatchRequest(IncrementCountActionName, { amount: 7 });
  }

  async ConfigureBlazor() {
    if (this.executionSideValue === null) {
      localStorage.setItem(this.executionSideKey, "To force a side set this to client/server");
    }

    const clientSideBlazorScript = "/_framework/blazor.webassembly.js";
    const serverSideBlazorScript = "/_framework/blazor.server.js";
    const executionSides = { client: "client", server: "server" };
    let source: string;

    if (this.executionSideValue === executionSides.client) {
      source = clientSideBlazorScript;
    } else if (this.executionSideValue === executionSides.server) {
      source = serverSideBlazorScript;
    } else {
      source = this.clientLoaded ? clientSideBlazorScript : serverSideBlazorScript;
    }
    console.log(`Using script: ${source}`);
    let module = await import(source);
    window.Blazor.start();
  }

  LoadClient() {
    if (!window._TimeWarpBlazor_?.clientLoaded) {
      localStorage.setItem(this.clientApplicationKey, this.applicationVersion);
      var iframe = document.createElement("iframe");
      iframe.setAttribute("id", "loaderFrame");
      iframe.setAttribute("style", "width:0; height:0; border:0; border:none");
      document.body.appendChild(iframe);
      const iframeSource = window.location.href;
      iframe.setAttribute("src", iframeSource);
    }
  }
}