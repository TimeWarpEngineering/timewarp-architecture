#region Purpose
// Constants and tiny shared helpers for `dev entra`: user-secret keys, defaults, masking, invocation display.
#endregion

#region Design
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
// Client secrets are never formatted into an invocation string; MaskSecret is the only display form.
// TenantDisplayNameKey / TenantDomainKey are the SSOT strings for informational user-secrets
// written by setup and shown by status (see EntraTenants for formatting/selection).
// Redirect URIs, user-secrets parsing, client-secret decisions, and az JSON readers live in
// EntraRedirectUris, EntraUserSecrets, EntraClientSecrets, and EntraAzJson.
#endregion

namespace DevCli.Services;

using System.Globalization;
using System.Text;

internal static class EntraSetup
{
  internal const string DefaultDisplayName = "TimeWarp Architecture Dev";
  internal const string CallbackPath = "/signin-oidc";
  internal const string WebServerHttpsRedirect = "https://localhost:63611/signin-oidc";
  internal const string IngressHttpsRedirect = "https://localhost:63610/signin-oidc";
  internal const string WebServerProject =
    "source/container-apps/web/projects/web-server/web-server.csproj";
  internal const string MaskedSecret = "********";

  internal const string SignInMethodsPrefix = "SignInOptions:Methods:";
  internal const string Microsoft365MethodName = "Microsoft365";
  internal const string WebServerAppSettings =
    "source/container-apps/web/projects/web-server/appsettings.json";

  internal const string PasskeyMethodKey = "SignInOptions:Methods:0";
  internal const string Microsoft365MethodKey = "SignInOptions:Methods:1";
  internal const string TenantIdKey = "Authentication:Entra:TenantId";
  internal const string TenantDisplayNameKey = "Authentication:Entra:TenantDisplayName";
  internal const string TenantDomainKey = "Authentication:Entra:TenantDomain";
  internal const string ClientIdKey = "Authentication:Entra:ClientId";
  internal const string ClientSecretKey = "Authentication:Entra:ClientSecret";
  internal const string ObsoleteTrustedTenants0Key = "Authentication:Entra:TrustedTenants:0";
  internal const string AllowBootstrapKey = "Authentication:Entra:AllowBootstrap";
  internal const string PublicOriginKey = "Authentication:Entra:PublicOrigin";
  internal const string ReseedSiteSettingsKey = "Authentication:Entra:ReseedSiteSettings";

  internal static string MaskSecret(string? secret)
  {
    return string.IsNullOrEmpty(secret) ? "(not set)" : MaskedSecret;
  }

  internal static string CredentialDisplayName(DateTimeOffset timestamp) =>
    $"dev-{timestamp.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

  internal static string FormatInvocation(string executable, IReadOnlyList<string> arguments)
  {
    StringBuilder builder = new();
    builder.Append(executable);
    foreach (string argument in arguments)
    {
      builder.Append(' ');
      builder.Append(QuoteArgument(argument));
    }

    return builder.ToString();
  }
  private static string QuoteArgument(string argument)
  {
    if (argument.Length == 0 || argument.Any(static ch => char.IsWhiteSpace(ch) || ch is '"' or '\''))
    {
      return "\"" + argument.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    return argument;
  }
}
