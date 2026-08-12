#!/usr/bin/env -S dotnet --
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058
#:property DefineConstants=$(DefineConstants);api

// Co-located Jaribu tests for markdown Accept negotiation (task 104-018).
// Real-host smoke: twin URL + Accept rewrite on / + browser Accept falls through.
// Run standalone: dotnet run source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs

#region Purpose
// Curl-verifiable proof that markdown twins and Accept: text/markdown negotiation work on the host.
#endregion

#region Design
// HTTP smoke only (no direct web-server ProjectReference): HostGraphFactory C-create brings the
// production pipeline. PreferMarkdown unit cases live as behavioral checks via Accept headers
// here rather than type-referencing MarkdownContentNegotiation (avoids standalone NU1107 from
// Oakton's Hosting <10 ceiling when web-server is a direct runfile project). Isolated HttpClient
// per call — no shared cookie jar.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.AgentDiscovery
{

  using System.Net;
  using System.Net.Http;
  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("AgentDiscovery")]
  public class MarkdownContentNegotiationHttp_Given_
  {
    private static HostGraph? Graph;
    private static WebTestServerApplication Web => Graph!.Web!;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<MarkdownContentNegotiationHttp_Given_>();

    public static async Task SetupOnce()
    {
#if(api)
      Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
      Graph = await HostGraphFactory.CreateWebAsync();
#endif
    }

    public static async Task CleanUpOnce()
    {
      if (Graph is not null)
      {
        await Graph.DisposeAsync();
        Graph = null;
      }
    }

    public static async Task IndexMdTwin_Should_ReturnMarkdown()
    {
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      using HttpResponseMessage response = await client.GetAsync("/index.md");

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      response.Content.Headers.ContentType.ShouldNotBeNull();
      response.Content.Headers.ContentType!.MediaType.ShouldBe("text/markdown");
      string body = await response.Content.ReadAsStringAsync();
      body.ShouldContain("TimeWarp.Architecture");
      body.ShouldContain("markdown twin");
    }

    public static async Task RootWithAcceptMarkdown_Should_ReturnMarkdownTwin()
    {
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      using var request = new HttpRequestMessage(HttpMethod.Get, "/");
      request.Headers.Accept.ParseAdd("text/markdown");

      using HttpResponseMessage response = await client.SendAsync(request);

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      response.Content.Headers.ContentType.ShouldNotBeNull();
      response.Content.Headers.ContentType!.MediaType.ShouldBe("text/markdown");
      string body = await response.Content.ReadAsStringAsync();
      body.ShouldContain("TimeWarp.Architecture");
      body.ShouldContain("markdown twin");
      // Negotiated responses must advertise Vary: Accept for caches.
      response.Headers.Vary.ShouldContain("Accept");
    }

    public static async Task RootWithAcceptHtml_Should_NotServeMarkdownBody()
    {
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      using var request = new HttpRequestMessage(HttpMethod.Get, "/");
      request.Headers.Accept.ParseAdd("text/html");

      using HttpResponseMessage response = await client.SendAsync(request);

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      string? mediaType = response.Content.Headers.ContentType?.MediaType;
      // Blazor/HTML path — must not be the markdown twin content-type.
      mediaType.ShouldNotBe("text/markdown");
      string body = await response.Content.ReadAsStringAsync();
      body.ShouldNotContain("markdown twin");
    }

    public static async Task AuthMd_Should_StillServeMarkdown()
    {
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      using HttpResponseMessage response = await client.GetAsync("/auth.md");

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      response.Content.Headers.ContentType.ShouldNotBeNull();
      response.Content.Headers.ContentType!.MediaType.ShouldBe("text/markdown");
      string body = await response.Content.ReadAsStringAsync();
      body.ShouldContain("passkey");
    }

    public static async Task BrowserLikeAccept_Should_NotServeMarkdownBody()
    {
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      using var request = new HttpRequestMessage(HttpMethod.Get, "/");
      request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

      using HttpResponseMessage response = await client.SendAsync(request);

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      response.Content.Headers.ContentType?.MediaType.ShouldNotBe("text/markdown");
      string body = await response.Content.ReadAsStringAsync();
      body.ShouldNotContain("markdown twin");
    }
  }
}
