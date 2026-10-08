// #region Purpose
// Registers the page's catalog tools with the browser model context so an external agent can call them.
// #endregion
//
// #region Design
// document.modelContext.registerTool is the current WebMCP entry, with an AbortSignal so the next
// page replaces the previous tools. navigator.modelContext is the older alias. provideContext is
// the earlier one-shot API. When none of those exist the call returns available false and
// registered 0. execute posts the arguments to .NET InvokeTool and parses the JSON result.
// The server still authorizes every endpoint the store handler calls.
// #endregion

interface WebMcpHost {
  invokeMethodAsync(methodName: string, name: string, argumentsJson: string): Promise<string>;
}

interface WebMcpTool {
  name: string;
  description: string;
  inputSchemaJson: string;
}

interface WebMcpRegisteredTool {
  name: string;
  description: string;
  inputSchema: unknown;
  execute: (input: Record<string, unknown> | undefined) => Promise<unknown>;
}

interface WebMcpModelContext {
  registerTool?: (tool: WebMcpRegisteredTool, options?: { signal?: AbortSignal }) => void;
  provideContext?: (context: { tools: WebMcpRegisteredTool[] }) => void;
}

interface WebMcpApplyResult {
  available: boolean;
  registered: number;
}

let controller: AbortController | null = null;

export async function Replace(host: WebMcpHost, tools: WebMcpTool[]): Promise<WebMcpApplyResult> {
  const modelContext = resolveModelContext();
  if (!modelContext) {
    return { available: false, registered: 0 };
  }

  const registeredTools = tools.map((tool) => toRegisteredTool(host, tool));
  if (typeof modelContext.registerTool === "function") {
    controller?.abort();
    controller = new AbortController();
    const signal = controller.signal;
    for (const tool of registeredTools) {
      modelContext.registerTool(tool, { signal });
    }

    return { available: true, registered: registeredTools.length };
  }

  if (typeof modelContext.provideContext === "function") {
    modelContext.provideContext({ tools: registeredTools });
    return { available: true, registered: registeredTools.length };
  }

  return { available: false, registered: 0 };
}

function toRegisteredTool(host: WebMcpHost, tool: WebMcpTool): WebMcpRegisteredTool {
  return {
    name: tool.name,
    description: tool.description,
    inputSchema: JSON.parse(tool.inputSchemaJson) as unknown,
    execute: async (input) => {
      const json = await host.invokeMethodAsync("InvokeTool", tool.name, JSON.stringify(input ?? {}));
      return JSON.parse(json) as unknown;
    },
  };
}

function resolveModelContext(): WebMcpModelContext | undefined {
  const documentContext = (document as Document & { modelContext?: WebMcpModelContext }).modelContext;
  if (documentContext) {
    return documentContext;
  }

  return (navigator as Navigator & { modelContext?: WebMcpModelContext }).modelContext;
}
