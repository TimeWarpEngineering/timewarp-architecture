#region Purpose
// Registers the xAI chat options and the server upstream. Does not register an unkeyed IChatClient.
#endregion

#region Design
// web-server composes Web.Spa.Program.ConfigureServices, which registers the browser relay as the
// unkeyed IChatClient. Putting the key-bearing client in that slot would let prerender resolve
// it. The handler depends on IAgentChatUpstream instead. Options bind the XAI section; a missing
// key leaves ApiKey null and startup succeeds. The fake flag is not given a default in appsettings.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features.AgentChats.Application;

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
    services.AddSingleton<IAgentChatUpstream, XaiChatUpstream>();
  }
}
