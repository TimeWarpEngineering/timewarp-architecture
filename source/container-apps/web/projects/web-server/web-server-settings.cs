#region Purpose
// Validated options Web.Server binds at startup.
#endregion

#region Design
// Every options type validates on start, so a bad configuration fails boot instead of the first
// request. TIP_* environment variables overlay TipOptions after the section bind.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using TimeWarp.Architecture.Abuse;
using TimeWarp.Architecture.Features.Identity.Application;

/// <summary>Registers Web.Server's validated options.</summary>
internal static class WebServerSettings
{
  /// <summary>Binds and validates each options section.</summary>
  internal static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection
      .AddFluentValidatedOptions<SignInOptions, SignInOptionsValidator>(configuration)
      .PostConfigure<IConfiguration>((options, configuration) => options.ApplyConfiguredMethods(configuration))
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<SampleOptions, SampleOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<SiteMetadata, SiteMetadataValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<WebAuthnOptions, WebAuthnOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<AgentTokenOptions, AgentTokenOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<MeteredCapabilityOptions, MeteredCapabilityOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<TipOptions, TipOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<AbuseRateLimitOptions, AbuseRateLimitOptionsValidator>(configuration)
      .ValidateOnStart();

    // TIP_* env overlay (timewarp-software parity) after section bind; strict TIP_ENABLED=="true".
    serviceCollection.PostConfigure<TipOptions>(static options => TipEnvironment.ApplyFromEnvironment(options));
  }
}
