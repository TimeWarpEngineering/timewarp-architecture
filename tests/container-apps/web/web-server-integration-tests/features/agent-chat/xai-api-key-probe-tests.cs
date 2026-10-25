#region Purpose
// The save-time xAI check drops the key from a refusal and does not treat a network failure as success.
#endregion

#region Design
// A stub HttpMessageHandler stands in for api.x.ai. The probe is the server type, visible here
// through InternalsVisibleTo. Nothing in the assertion is the submitted key.
#endregion

namespace XaiApiKeyProbe_;

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.AgentChats.Application;

public class XaiApiKeyProbe_Given_
{
  private const string Secret = "xai-probe-key-399-zzzz";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<XaiApiKeyProbe_Given_>();

  public static async Task Rejection_Scrubs_The_Key()
  {
    await using ServiceProvider provider = Build(
      HttpStatusCode.Unauthorized,
      "invalid " + Secret + "\ntry again");
    IXaiApiKeyProbe probe = provider.GetRequiredService<IXaiApiKeyProbe>();

    OneOf<bool, SharedProblemDetails> result = await probe.ValidateAsync(Secret, CancellationToken.None);

    result.IsT1.ShouldBeTrue();
    result.AsT1.Status.ShouldBe(400);
    result.AsT1.Detail.ShouldNotBeNull();
    result.AsT1.Detail!.ShouldContain("[redacted]");
    result.AsT1.Detail.ShouldNotContain(Secret);
    result.AsT1.Detail.ShouldNotContain("\n");
  }

  public static async Task Network_Failure_Does_Not_Accept_The_Key()
  {
    await using ServiceProvider provider = Build(
      HttpStatusCode.OK,
      "",
      new HttpRequestException("connection refused"));
    IXaiApiKeyProbe probe = provider.GetRequiredService<IXaiApiKeyProbe>();

    OneOf<bool, SharedProblemDetails> result = await probe.ValidateAsync(Secret, CancellationToken.None);

    result.IsT1.ShouldBeTrue();
    result.AsT1.Status.ShouldBe(503);
    result.AsT1.Detail.ShouldBe("xAI could not be reached. Nothing was saved.");
    result.AsT1.Detail!.ShouldNotContain(Secret);
  }

  private static ServiceProvider Build(HttpStatusCode status, string body, Exception? error = null)
  {
    ServiceCollection services = new();
    services.AddHttpClient(XaiApiKeyProbe.HttpClientName)
      .ConfigurePrimaryHttpMessageHandler(() => new FixedHandler(status, body, error));
    services.AddOptions<XaiChatOptions>()
      .Configure(options => options.Endpoint = XaiChatDefaults.DefaultEndpoint);
    services.AddSingleton<IXaiApiKeyProbe, XaiApiKeyProbe>();
    return services.BuildServiceProvider();
  }

  private sealed class FixedHandler(HttpStatusCode status, string body, Exception? error) : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      _ = request;
      cancellationToken.ThrowIfCancellationRequested();
      if (error is not null)
      {
        throw error;
      }

      return Task.FromResult(new HttpResponseMessage(status)
      {
        Content = new StringContent(body)
      });
    }
  }
}
