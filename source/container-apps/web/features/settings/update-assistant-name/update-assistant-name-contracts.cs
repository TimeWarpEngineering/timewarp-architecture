#region Purpose
// Authenticated rename of the in-app assistant, with the same optimistic concurrency as site settings.
#endregion

#region Design
// The name is a SiteSettings column, but it is an AI concern. A dedicated command keeps the AI
// page from round-tripping Entra policy, the passkey prompt, and the accepted-tenant list.
// SettingsWrite, the same policy as UpdateSiteSettings. Version is the concurrency token from
// GetSiteSettings; mismatch is 409. The validator calls AssistantNameRules, the same check
// SiteSettings.RenameAssistant runs. PUT api/settings/assistant sits beside the anonymous GET
// of the name and avatar. ICommand, not IIdempotentCommand: a successful write advances Version,
// so a retry of the same body conflicts. [Operation] publishes the name to CLI and MCP.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.SettingsWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class UpdateAssistantName
{
  [ApiRoute("api/settings/assistant", HttpVerb.Put)]
  [Operation
  (
    "assistant-name.update",
    Description = "Rename the in-app assistant.",
    Group = "admin.ai",
    Title = "Update assistant name"
  )]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    public string AssistantName { get; set; } = null!;
    public long Version { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.AssistantName)
        .NotEmpty()
        .WithMessage("Assistant name is required.");
      RuleFor(command => command.AssistantName)
        .Must(AssistantNameRules.IsValid)
        .When(static command => !string.IsNullOrWhiteSpace(command.AssistantName))
        .WithMessage
        (
          $"Assistant name must be 1-{AssistantNameRules.MaxLength} characters of plain text, "
          + "without markup, control, format or line-separator characters."
        );
      RuleFor(command => command.Version).GreaterThanOrEqualTo(0);
    }
  }

  public sealed class Response
  {
    public string AssistantName { get; }
    public long Version { get; }

    public Response(string assistantName, long version)
    {
      AssistantName = Guard.Against.NullOrWhiteSpace(assistantName);
      Version = version;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response(AssistantNameRules.Default, 1);
}
