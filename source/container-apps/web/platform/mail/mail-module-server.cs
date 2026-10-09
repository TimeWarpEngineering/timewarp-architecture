#region Purpose
// Registers the configured mail sender and the public base-URL accessor.
#endregion

#region Design
// Mail:Sender selects the sender. "Development" (set in appsettings.Development.json) registers
// DevelopmentEmailSender; any other value or no value registers UnconfiguredEmailSender, which
// throws so a deployed app without a real provider reports EmailCopySent=false instead of claiming
// a delivery that never happened. A real provider adds its own Mail:Sender value and registration
// here. Tests that need a fake construct IEmailSender themselves and do not boot this module.
#endregion

namespace TimeWarp.Architecture.Mail;

using TimeWarp.Modules;

public sealed class MailModule : IModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    IConfigurationSection section = configuration.GetSection(MailOptions.SectionName);
    serviceCollection.Configure<MailOptions>(section);
    serviceCollection.AddHttpContextAccessor();
    if (string.Equals(section[nameof(MailOptions.Sender)], MailOptions.DevelopmentSender, StringComparison.Ordinal))
    {
      serviceCollection.AddSingleton<IEmailSender, DevelopmentEmailSender>();
    }
    else
    {
      serviceCollection.AddSingleton<IEmailSender, UnconfiguredEmailSender>();
    }

    serviceCollection.AddSingleton<IAppBaseUrlAccessor, HttpAppBaseUrlAccessor>();
  }
}
