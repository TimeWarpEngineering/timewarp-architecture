#region Purpose
// Browser proof that Agent links explains linking, shows the account id, and copies the openssl prompt.
#endregion

#region Design
// Same host as the other WASM proofs: server UseMock from this project's appsettings, wwwroot
// UseMock left false, so the page's account id is the identity-session principal claim. The
// first passkey account is Administrator and holds agent-link.manage.self. Clipboard permissions
// are granted so the copy buttons can be read back. The prompt carries that id and the
// ceremony text, and must not send the person to tools/ or a dotnet client. Task 431: the
// prompt opens from the compact page into AgentLinkPromptModal and is read from there.
#endregion

namespace AgentLinksOnboardingPlaywright_;

using System.Text.Json;
using TimeWarp.Architecture.Features.AgentLinks;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;

public class AgentLinksOnboarding_Given_Wasm
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AgentLinksOnboarding_Given_Wasm>();

  public static async Task Page_Should_ShowTheAccountId_And_CopyThePrompt()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
      Locale = "en-US",
      Permissions = ["clipboard-read", "clipboard-write"],
    });
    IPage page = await context.NewPageAsync();

    await SignInWithNewPasskeyAsync(page, context);
    string origin = new Uri(InProcTestPorts.WebHostUrl).GetLeftPart(UriPartial.Authority);
    await page.GotoAsync(
      origin + "/AgentLinks",
      new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });

    ILocator accountId = page.Locator("[data-qa=AccountId]");
    await accountId.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    string id = (await accountId.InnerTextAsync()).Trim();
    Guid.TryParse(id, out Guid parsed).ShouldBeTrue();
    parsed.ShouldNotBe(Guid.Empty);

    string explanation = await page.Locator("[data-qa=LinkingExplanation]").InnerTextAsync();
    explanation.ShouldContain("Paid service does not require a linked human.");
    explanation.ShouldContain("approve or deny");
    explanation.ShouldContain("timewarp.humanUx/v1");
    explanation.ShouldContain("GetHumanUx");

    // Copy the account id while the page is not covered by the modal (task 431).
    await page.Locator("[data-qa=CopyAccountId]").ClickAsync();
    await Assertions.Expect(page.Locator("[data-qa=CopyAccountId]")).ToHaveAttributeAsync("data-copied", "true", new() { Timeout = 15_000 });
    string copiedId = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
    copiedId.ShouldBe(id);

    string prompt = await OpenPromptAndReadAsync(page);
    prompt.ShouldContain(id);
    prompt.ShouldContain(origin);
    prompt.ShouldContain("openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256");
    prompt.ShouldContain("openssl dgst -sha256 -sign");
    JsonNamingPolicy namingPolicy = ContractSerializationDefaults.Options.PropertyNamingPolicy
      ?? throw new InvalidOperationException("Contract serialization has no property naming policy.");
    const string humanUx = GetHumanUx.Query.RouteTemplate;
    int open = humanUx.IndexOf('{', StringComparison.Ordinal);
    int close = humanUx.IndexOf('}', StringComparison.Ordinal);
    string humanUxShell = string.Concat(humanUx.AsSpan(0, open), "${link_id}", humanUx.AsSpan(close + 1));

    prompt.ShouldContain(AgentKeyProof.GetPrefix(AgentKeyCeremonyType.Registration));
    prompt.ShouldContain(AgentKeyProof.GetPrefix(AgentKeyCeremonyType.TokenIssuance));
    prompt.ShouldContain(AgentScopes.IdentityRead);
    prompt.ShouldContain(StartAgentKeyRegistration.Command.RouteTemplate);
    prompt.ShouldContain(CompleteAgentKeyRegistration.Command.RouteTemplate);
    prompt.ShouldContain(StartAgentTokenIssuance.Command.RouteTemplate);
    prompt.ShouldContain(CompleteAgentTokenIssuance.Command.RouteTemplate);
    prompt.ShouldContain(RequestAgentHumanLink.Command.RouteTemplate);
    prompt.ShouldContain(humanUxShell);
    prompt.ShouldContain(namingPolicy.ConvertName(nameof(RequestAgentHumanLink.Command.HumanPrincipalId)));
    prompt.ShouldContain(GetHumanUx.Response.SpecId);
    prompt.ShouldContain("mints a fresh token");
    prompt.ShouldNotContain("same bearer");
    prompt.ShouldNotContain("{LinkId");
    prompt.ShouldContain("curl");
    prompt.ShouldNotContain("tools/");
    prompt.ShouldNotContain("dotnet");

    await page.Locator("[data-qa=CopyAgentPrompt]").ClickAsync();
    await Assertions.Expect(page.Locator("[data-qa=CopyAgentPrompt]")).ToHaveAttributeAsync("data-copied", "true", new() { Timeout = 15_000 });
    string copiedPrompt = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
    copiedPrompt.ShouldBe(prompt);

    _ = graph;
  }

  private static async Task<string> OpenPromptAndReadAsync(IPage page)
  {
    // Task 431: the prompt is compact on the page and lives in AgentLinkPromptModal.
    await page.Locator("[data-qa=OpenAgentLinkPrompt]").ClickAsync();
    ILocator promptLocator = page.Locator("[data-qa=AgentLinkPrompt]");
    await promptLocator.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    return (await promptLocator.InnerTextAsync()).Trim();
  }

  private static async Task SignInWithNewPasskeyAsync(IPage page, IBrowserContext context)
  {
    ICDPSession cdp = await context.NewCDPSessionAsync(page);
    await cdp.SendAsync("WebAuthn.enable");
    await cdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
    {
      ["options"] = new Dictionary<string, object>
      {
        ["protocol"] = "ctap2",
        ["transport"] = "internal",
        ["hasResidentKey"] = true,
        ["hasUserVerification"] = true,
        ["isUserVerified"] = true,
        ["automaticPresenceSimulation"] = true,
      },
    });

    await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    await page.Locator("[data-qa=HomeSignIn]").ClickAsync();
    await page.WaitForURLAsync("**/Login**", new PageWaitForURLOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=CreatePasskey]").ClickAsync();
    await page.WaitForURLAsync("**/Settings", new PageWaitForURLOptions { Timeout = 60_000 });
    await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
  }
}
