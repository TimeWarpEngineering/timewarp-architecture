#region Purpose
// Registers the xAI chat options and the server upstream. Does not register an unkeyed IChatClient.
#endregion

#region Design
// web-server composes Web.Spa.Program.ConfigureServices, which registers the browser relay as the
// unkeyed IChatClient. Putting the key-bearing client in that slot would let prerender resolve
// it. The handler depends on IAgentChatUpstream instead. Options bind the XAI section; a missing
// key leaves ApiKey null and startup succeeds. The fake flag is not given a default in appsettings.
// The protected-secret store is TryAdd: PostgresDbModule has already registered the EF store when
// a connection string exists. Otherwise the unavailable store stays, and Admin → AI is host-only.
// The chat-client factory is TryAdd so a test can capture the key Ask would send.
// The probe, the user key, and the resolver are TryAdd. Settings and the current principal are
// optional constructor parameters, so a host that has neither still resolves host-then-site.
// The resolver logger is optional too. A missing logger still treats an unreadable mode as Required.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeWarp.Architecture.Features.AgentChats.Application;
using TimeWarp.Architecture.Secrets;

/// <summary>Wires the server-side chat relay.</summary>
public static class XaiChatRegistration
{
  /// <summary>Adds options, admission, and <see cref="IAgentChatUpstream"/>.</summary>
  public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);
    services
      .AddOptions<XaiChatOptions>()
      .Bind(configuration.GetSection(XaiChatOptions.SectionName));
    services.AddSingleton<AgentChatAdmission>();
    services.TryAddSingleton<IProtectedSecretStore, UnavailableProtectedSecretStore>();
    services.TryAddSingleton<IXaiChatClientFactory, OpenAiXaiChatClientFactory>();
    services.AddHttpClient(XaiApiKeyProbe.HttpClientName);
    services.TryAddSingleton<IXaiApiKeyProbe, XaiApiKeyProbe>();
    services.TryAddScoped<ISiteXaiApiKey, SiteXaiApiKey>();
    services.TryAddScoped<IUserXaiApiKey, UserXaiApiKey>();
    services.TryAddScoped<IXaiApiKeyResolver, XaiApiKeyResolver>();
    services.AddSingleton<IAgentChatUpstream, XaiChatUpstream>();
  }
}
