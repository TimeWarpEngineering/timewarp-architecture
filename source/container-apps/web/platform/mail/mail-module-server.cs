#region Purpose
// Registers the development mail sender and the request base-URL accessor.
#endregion

#region Design
// Always registered. There is no production provider in this host. Tests that need a fake
// construct IEmailSender themselves and do not boot this module.
#endregion

namespace TimeWarp.Architecture.Mail;

using TimeWarp.Modules;

public sealed class MailModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.Configure<MailOptions>(configuration.GetSection(MailOptions.SectionName));
    serviceCollection.AddHttpContextAccessor();
    serviceCollection.AddSingleton<IEmailSender, DevelopmentEmailSender>();
    serviceCollection.AddSingleton<IAppBaseUrlAccessor, HttpAppBaseUrlAccessor>();
  }
}
