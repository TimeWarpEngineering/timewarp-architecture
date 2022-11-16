"use strict";
class BlazorDualMode {
    ApplicationVersion = "TimeWarp.Architecture.0.0.1";
    ClientApplicationKey = "clientApplication";
    ExecutionSideKey = "executionSide";
    ClientApplicationValue = localStorage.getItem(this.ClientApplicationKey);
    ExecutionSideValue = localStorage.getItem(this.ExecutionSideKey);
    ClientLoaded = this.ClientApplicationValue === this.ApplicationVersion;
    async ConfigureBlazor() {
        if (this.ExecutionSideValue === null) {
            localStorage.setItem(this.ExecutionSideKey, "To force a side set this to client/server");
        }
        const clientSideBlazorScript = "/_framework/blazor.webassembly.js";
        const serverSideBlazorScript = "/_framework/blazor.server.js";
        const executionSides = { client: "client", server: "server" };
        let source;
        if (this.ExecutionSideValue === executionSides.client) {
            source = clientSideBlazorScript;
        }
        else if (this.ExecutionSideValue === executionSides.server) {
            source = serverSideBlazorScript;
        }
        else {
            source = this.ClientLoaded
                ? clientSideBlazorScript
                : serverSideBlazorScript;
        }
        console.log(`Using script: ${source}`);
        await import(source);
        console.log("script imported");
        window.Blazor.start();
        console.log("Blazor started");
    }
    // Called from C#
    LoadClient() {
        if (!window.BlazorDualMode?.ClientLoaded) {
            console.log("BlazorDualMode is loading client");
            localStorage.setItem(this.ClientApplicationKey, this.ApplicationVersion);
            const iframe = document.createElement("iframe");
            iframe.setAttribute("id", "loaderFrame");
            iframe.setAttribute("style", "width:0; height:0; border:0; border:none");
            document.body.appendChild(iframe);
            const iframeSource = window.location.href;
            iframe.setAttribute("src", iframeSource);
        }
        else {
            console.log("Client previously loaded");
        }
    }
}
window.BlazorDualMode = new BlazorDualMode();
window.BlazorDualMode.ConfigureBlazor();
console.log("BlazorDualMode.ts loaded");
//# sourceMappingURL=BlazorDualMode.js.map