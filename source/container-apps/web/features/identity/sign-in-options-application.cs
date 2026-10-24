#region Purpose
// The server-owned list of enabled browser sign-in methods.
#endregion

#region Design
// Section name "SignInOptions" matches the type name: AddFluentValidatedOptions binds by type name.
// Methods is a collection of SignInMethod. The property initializer is both methods, which is the
// template default when the section is absent. The configuration binder appends to that list, so
// web-server PostConfigure calls ApplyConfiguredMethods and replaces it from the raw section.
// Checked-in appsettings lists Passkey only so a host with empty Entra settings still boots;
// listing Microsoft365 is what registers the Entra scheme. ConfigurationLists reads the raw
// section for scheme registration, which runs before options validation. A missing Methods
// section follows the initializer. The browser does not bind this type; it renders the offered
// list from GetEntraSignInOffered.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Configuration;

/// <summary>Enabled browser sign-in methods for one generated app.</summary>
public sealed class SignInOptions
{
  public const string SectionName = "SignInOptions";

  public List<SignInMethod> Methods { get; set; } = [SignInMethod.Passkey, SignInMethod.Microsoft365];

  public bool Lists(SignInMethod method) => Methods.Contains(method);

  /// <summary>
  /// Replaces <see cref="Methods"/> from configuration. A missing Methods section keeps both methods.
  /// Names that do not parse are left for <see cref="SignInOptionsValidator"/>.
  /// </summary>
  public void ApplyConfiguredMethods(IConfiguration configuration)
  {
    IConfigurationSection section = configuration.GetSection($"{SectionName}:Methods");
    if (!section.Exists())
    {
      Methods = [SignInMethod.Passkey, SignInMethod.Microsoft365];
      return;
    }

    List<SignInMethod> configured = [];
    foreach (IConfigurationSection child in section.GetChildren())
    {
      if (Enum.TryParse(child.Value, ignoreCase: false, out SignInMethod parsed) && Enum.IsDefined(parsed))
      {
        configured.Add(parsed);
      }
    }

    Methods = configured;
  }

  /// <summary>
  /// True when configuration lists <paramref name="method"/>. A missing Methods section uses the
  /// property initializer (Passkey and Microsoft365).
  /// </summary>
  public static bool ConfigurationLists(IConfiguration configuration, SignInMethod method)
  {
    IConfigurationSection section = configuration.GetSection($"{SectionName}:Methods");
    if (!section.Exists())
    {
      return method is SignInMethod.Passkey or SignInMethod.Microsoft365;
    }

    foreach (IConfigurationSection child in section.GetChildren())
    {
      if (Enum.TryParse(child.Value, ignoreCase: false, out SignInMethod parsed)
        && Enum.IsDefined(parsed)
        && parsed == method)
      {
        return true;
      }
    }

    return false;
  }
}
